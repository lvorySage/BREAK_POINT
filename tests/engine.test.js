import test from 'node:test';
import assert from 'node:assert/strict';
import {
  STAGES, RULES, createMatch, startMatch, redAct, blueAct, tick, nextRound, rematch, buildSignals,
  blueGrade, redGrade, financialReport, viewFor, outcomeFor, EngineError,
} from '../src/incident/engine.js';

const T0 = 1_000_000;
const best = (s) => [...s.signals].sort((a, b) => b.priority - a.priority).map((a) => a.id);
const worst = (s) => [...s.signals].sort((a, b) => a.priority - b.priority).map((a) => a.id);

/** Plays a whole match. redPick(stageIdx) → {primary, side}; blueOrder(state) → ids. */
function play(redPick, blueOrder) {
  let s = startMatch(createMatch({ code: 'TEST1' }), T0);
  let now = T0;
  for (let r = 0; r < RULES.rounds; r++) {
    s = redAct(s, redPick(r), (now += 5000));
    s = blueAct(s, { order: blueOrder(s) }, (now += 5000));
    if (r < RULES.rounds - 1) s = nextRound(s, (now += 1000));
  }
  return s;
}
const biggest = (r) => ({
  primary: [...STAGES[r].main].sort((a, b) => b.leak - a.leak || b.heat - a.heat)[0].id,
  side: r === 3 ? 'forward' : [...STAGES[r].side].sort((a, b) => b.leak - a.leak || b.heat - a.heat)[0].id,
});
const first = (r) => ({ primary: STAGES[r].main[0].id, side: STAGES[r].side[0].id });

test('four mandatory rounds even after a perfect early defence', () => {
  const s = play(first, best);
  assert.equal(s.history.length, 4);
  assert.equal(s.status, 'complete');
  assert.ok(s.history[0].mainContained);
});

test('every Red combination builds three distinct, rankable alerts', () => {
  STAGES.forEach((st, i) => st.main.forEach((m) => st.side.forEach((d) => {
    const sig = buildSignals(i, m.id, d.id);
    assert.equal(sig.length, 3);
    assert.deepEqual(sig.map((a) => a.id), ['e1', 'e2', 'e3']);
    assert.equal(new Set(sig.map((a) => a.key)).size, 3);
    assert.deepEqual(sig.map((a) => a.source).sort(), ['ambient', 'main', 'side']);
  })));
});

test('the main attack does not always sit in the same card position', () => {
  const positions = new Set();
  STAGES.forEach((st, i) => st.main.forEach((m) => st.side.forEach((d) => {
    positions.add(buildSignals(i, m.id, d.id).findIndex((a) => a.source === 'main'));
  })));
  assert.equal(positions.size, 3);
});

test('wrong alert order causes measurable late-response harm', () => {
  const good = play(biggest, best);
  const bad = play(biggest, worst);
  assert.equal(good.extracted, 0);
  assert.ok(bad.extracted > 0);
  assert.ok(bad.priorityMistakes > good.priorityMistakes);
});

test('timeouts apply the fallback choices', () => {
  let s = startMatch(createMatch(), T0);
  assert.equal(tick(s, T0 + 29_999), s, 'no-op before the deadline');
  s = tick(s, T0 + 30_000);
  assert.equal(s.status, 'blue');
  assert.deepEqual([s.pendingRed.primary, s.pendingRed.side], [STAGES[0].main[0].id, STAGES[0].side[0].id]);
  assert.ok(s.pendingRed.timedOut);
  s = tick(s, s.deadline);
  assert.equal(s.status, 'resolved');
  assert.deepEqual(s.lastResolution.order, ['e1', 'e2', 'e3']);
  assert.ok(s.lastResolution.blueTimedOut);
});

test('Blue must rank exactly three unique alerts', () => {
  let s = redAct(startMatch(createMatch(), T0), first(0), T0);
  assert.throws(() => blueAct(s, { order: ['e1', 'e2'] }), EngineError);
  assert.throws(() => blueAct(s, { order: ['e1', 'e1', 'e2'] }), EngineError);
  assert.throws(() => blueAct(s, { order: ['e1', 'e2', 'x9'] }), EngineError);
  assert.throws(() => redAct(s, first(0)), EngineError, 'Red cannot act on Blue’s turn');
});

test('Blue view hides hidden priority, truth and which card is Red’s', () => {
  const s = redAct(startMatch(createMatch(), T0), first(0), T0);
  const v = viewFor(s, 'blue');
  assert.equal(v.signals.length, 3);
  v.signals.forEach((a) => {
    ['priority', 'correct', 'leak', 'heat', 'source', 'key'].forEach((k) => assert.ok(!(k in a), k));
  });
  assert.ok(!('pendingRed' in v));
  assert.ok(!('signals' in viewFor(s, 'red')));
});

test('rematch resets the score but keeps the match code', () => {
  const s = play(biggest, worst);
  const r = rematch(s, T0);
  assert.equal(r.code, 'TEST1');
  assert.equal(r.status, 'red');
  assert.equal(r.extracted, 0);
  assert.equal(r.history.length, 0);
});

test('worst case: 100% simulated extraction is possible', () => {
  const s = play(biggest, worst);
  assert.equal(s.extracted, 100);
  assert.ok(s.history[3].cascade);
  assert.equal(s.outcome.code, 'full');
  assert.equal(redGrade(s), 'S');
  assert.equal(blueGrade(s), 'F');
});

test('perfect Blue ordering prevents all extraction for every Red strategy', () => {
  const picks = [first, biggest, (r) => ({ primary: STAGES[r].main[2].id, side: STAGES[r].side[2].id })];
  for (const p of picks) {
    const s = play(p, best);
    assert.equal(s.extracted, 0);
    assert.equal(s.triagePoints, 12);
    assert.equal(blueGrade(s), 'S');
    assert.equal(s.outcome.code, 'contained');
    assert.equal(financialReport(s).protected, RULES.datasetValueOmr);
  }
});

test('one late response cannot instantly cause full loss', () => {
  // Perfect in rounds 1–3, worst in round 4 with the biggest final attack.
  const s = play(biggest, (st) => (st.stage === 3 ? worst(st) : best(st)));
  assert.ok(s.extracted > 0);
  assert.ok(s.extracted < 100);
  assert.ok(!s.history[3].cascade);
});

test('rounds 1–2 never extract data', () => {
  let s = startMatch(createMatch(), T0);
  for (let r = 0; r < 2; r++) {
    s = blueAct(redAct(s, biggest(r), T0), { order: worst(redAct(s, biggest(r), T0)) }, T0);
    assert.equal(s.extracted, 0);
    s = nextRound(s, T0);
  }
});

test('extraction never exceeds 100% for any Red/Blue combination', () => {
  const perms = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
  for (let mi = 0; mi < 3; mi++) for (let si = 0; si < 3; si++) for (const p of perms) {
    const s = play((r) => ({ primary: STAGES[r].main[mi].id, side: STAGES[r].side[si].id }),
      (st) => p.map((i) => st.signals[i].id));
    assert.ok(s.extracted >= 0 && s.extracted <= 100, `${s.extracted}`);
    assert.ok(s.triagePoints >= 0 && s.triagePoints <= 12);
  }
});

test('outcome titles follow the extraction bands', () => {
  assert.equal(outcomeFor(0).title, 'THE DATA WAS PROTECTED');
  assert.equal(outcomeFor(1).title, 'PARTIAL DATA BREACH');
  assert.equal(outcomeFor(59).title, 'PARTIAL DATA BREACH');
  assert.equal(outcomeFor(60).title, 'MAJOR DATA BREACH');
  assert.equal(outcomeFor(99).title, 'MAJOR DATA BREACH');
  assert.equal(outcomeFor(100).title, 'COMPLETE DATA LOSS');
});
