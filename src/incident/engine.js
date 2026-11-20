/*
 * INCIDENT ATTACK — authoritative game engine.
 *
 * Pure functions, no React, no DOM. Every score, leak and grade is decided here;
 * screens only display it. Animations (rolling digits, rain, sirens) must never
 * feed back into these numbers.
 *
 * Flow: lobby → red → blue → resolved → red … (4 rounds, never ends early) → complete.
 */
import { STAGES, CASE_STUDIES } from '../../incident-attack/stages.js';

export { STAGES };

export const RULES = {
  rounds: 4,
  turnSeconds: 30,
  datasetValueOmr: 100000,
  extractionStartsAtStage: 2, // 0-based: rounds 3 and 4
  delayLeakRate: { slot2one: 0.36, slot3one: 0.48, two: 0.88 },
  heatRate: { one: 0.42, two: 0.9 },
  verifyDisruption: { first: 220, later: 60 },
  leaveDisruption: { first: 380, later: 0 },
  maxTriagePerRound: 3,
};

export class EngineError extends Error {}

// All 6 orderings of [main, side, ambient] → display positions.
const PERMS = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
const ROOM_CHARS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';

export function roomCode(rand = Math.random) {
  let c = '';
  for (let i = 0; i < 5; i++) c += ROOM_CHARS[Math.floor(rand() * ROOM_CHARS.length)];
  return c;
}

function fresh(code) {
  return {
    code,
    status: 'lobby', // lobby | red | blue | resolved | complete
    stage: 0,
    turn: null, // 'red' | 'blue'
    deadline: null,
    pendingRed: null,
    signals: [],
    history: [],
    lastResolution: null,
    extracted: 0,
    compromise: 0,
    disruption: 0,
    redPressure: 0,
    triagePoints: 0,
    priorityMistakes: 0,
    finishedAt: null,
    outcome: null,
  };
}

export function createMatch({ code = roomCode() } = {}) {
  return fresh(code);
}

export function startMatch(s, now = Date.now()) {
  if (s.status !== 'lobby' && s.status !== 'complete') throw new EngineError('Match already running');
  return beginRedTurn({ ...fresh(s.code), status: 'red' }, 0, now);
}

/** Rematch: same match code, everything else reset. */
export const rematch = (s, now = Date.now()) => startMatch({ ...s, status: 'complete' }, now);

function beginRedTurn(s, stage, now) {
  return { ...s, status: 'red', turn: 'red', stage, pendingRed: null, signals: [], deadline: now + RULES.turnSeconds * 1000 };
}

/* ------------------------------------------------------------------ alerts */

/**
 * The three SIEM alerts for a round: Red's main attack, Red's distraction and one
 * contextual background event. Card position depends on stage + choices only, so
 * the main attack is never always in the same slot.
 */
export function buildSignals(stageIdx, primaryId, sideId) {
  const st = STAGES[stageIdx];
  const mi = st.main.findIndex((c) => c.id === primaryId);
  const si = st.side.findIndex((c) => c.id === sideId);
  if (mi < 0 || si < 0) throw new EngineError('Unknown Red choice');
  const ai = (mi + 2 * si + stageIdx) % st.ambient.length;
  const sources = [
    { ...st.main[mi], source: 'main' },
    { ...st.side[si], source: 'side' },
    { ...st.ambient[ai], source: 'ambient' },
  ];
  const perm = PERMS[(mi * 3 + si + stageIdx * 2) % PERMS.length];
  const shown = [];
  perm.forEach((srcIdx, pos) => { shown[pos] = sources[srcIdx]; });
  return shown.map((a, i) => ({ ...a, key: a.id, id: 'e' + (i + 1) }));
}

/* ----------------------------------------------------------------- actions */

export function redAct(s, choice, now = Date.now(), timedOut = false) {
  if (s.status !== 'red') throw new EngineError('Not Red’s turn');
  const st = STAGES[s.stage];
  const primary = choice?.primary, side = choice?.side;
  const main = st.main.find((c) => c.id === primary);
  const dist = st.side.find((c) => c.id === side);
  if (!main || !dist) throw new EngineError('Pick one main attack and one distraction');
  return {
    ...s,
    status: 'blue',
    turn: 'blue',
    pendingRed: { primary, side, timedOut },
    signals: buildSignals(s.stage, primary, side),
    redPressure: s.redPressure + main.heat + dist.heat,
    deadline: now + RULES.turnSeconds * 1000,
  };
}

export function blueAct(s, choice, now = Date.now(), timedOut = false) {
  if (s.status !== 'blue') throw new EngineError('Not Blue’s turn');
  const order = choice?.order;
  const ids = s.signals.map((a) => a.id);
  if (!Array.isArray(order) || order.length !== 3) throw new EngineError('Rank exactly three alerts');
  if (new Set(order).size !== 3) throw new EngineError('Each alert can only be ranked once');
  if (!order.every((id) => ids.includes(id))) throw new EngineError('Unknown alert');
  return resolve(s, order, now, timedOut);
}

/** Applies timeouts. Call this on a timer; it is a no-op until the deadline passes. */
export function tick(s, now = Date.now()) {
  if (s.deadline == null || now < s.deadline) return s;
  if (s.status === 'red') {
    const st = STAGES[s.stage];
    return redAct(s, { primary: st.main[0].id, side: st.side[0].id }, now, true);
  }
  if (s.status === 'blue') return blueAct(s, { order: s.signals.map((a) => a.id) }, now, true);
  return s;
}

/** Moves from a resolved round to the next Red turn. */
export function nextRound(s, now = Date.now()) {
  if (s.status !== 'resolved') throw new EngineError('Round not resolved');
  return beginRedTurn(s, s.stage + 1, now);
}

/* -------------------------------------------------------------- resolution */

function delayRate(queue, bad) {
  if (bad >= 2) return RULES.delayLeakRate.two;
  if (bad === 1) return queue === 2 ? RULES.delayLeakRate.slot2one : RULES.delayLeakRate.slot3one;
  return 0;
}

export function triageScore(alerts) {
  let points = 0;
  for (let i = 0; i < alerts.length; i++) {
    for (let j = i + 1; j < alerts.length; j++) if (alerts[i].priority >= alerts[j].priority) points++;
  }
  return points;
}

function resolve(s, order, now, timedOut) {
  const stageIdx = s.stage;
  const byId = Object.fromEntries(s.signals.map((a) => [a.id, a]));
  const queue = order.map((id) => byId[id]);
  const extractionOn = stageIdx >= RULES.extractionStartsAtStage;

  let preliminary = 0, heatGained = 0, disruption = 0;
  const alerts = queue.map((a, i) => {
    const pos = i + 1;
    const bad = queue.slice(0, i).filter((b) => b.priority < a.priority).length;
    const wasDelayed = bad > 0;
    const r = {
      id: a.id, key: a.key, source: a.source, icon: a.icon, title: a.title, severity: a.severity,
      clue: a.clue, priority: a.priority, correct: a.correct,
      queue: pos, badBefore: bad, wasDelayed, contained: false, leak: 0, heat: 0, disruption: 0,
    };
    if (a.correct === 'stop') {
      r.contained = !wasDelayed;
      if (wasDelayed) {
        r.leak = extractionOn ? Math.ceil(a.leak * delayRate(pos, bad)) : 0;
        r.heat = Math.ceil(a.heat * (bad >= 2 ? RULES.heatRate.two : RULES.heatRate.one));
      }
    } else if (a.correct === 'verify') {
      r.disruption = pos === 1 ? RULES.verifyDisruption.first : RULES.verifyDisruption.later;
    } else {
      r.disruption = pos === 1 ? RULES.leaveDisruption.first : RULES.leaveDisruption.later;
    }
    preliminary += r.leak; heatGained += r.heat; disruption += r.disruption;
    return r;
  });

  const points = triageScore(queue);
  const mistakes = RULES.maxTriagePerRound - points;
  const compromise = s.compromise + heatGained;
  const remaining = 100 - s.extracted;

  let extractedNow = preliminary, multiplier = 1, cascade = false;
  if (stageIdx === 3) {
    const main = alerts.find((a) => a.source === 'main');
    const side = alerts.find((a) => a.source === 'side');
    multiplier = 0.7 + Math.min(0.55, compromise / 100);
    cascade = s.extracted >= 12 && s.priorityMistakes >= 6
      && s.pendingRed.primary === 'large' && s.pendingRed.side === 'forward'
      && main.queue === 3 && main.wasDelayed && side.wasDelayed && compromise >= 18;
    extractedNow = cascade ? remaining : Math.ceil(preliminary * multiplier);
  }
  // Without the rare final-round cascade, total loss stops at 99%.
  extractedNow = Math.max(0, Math.min(extractedNow, cascade ? remaining : 99 - s.extracted));
  if (!extractionOn) extractedNow = 0;

  const ideal = [...queue].sort((a, b) => b.priority - a.priority).map((a) => a.id);
  const entry = {
    stage: stageIdx,
    red: { ...s.pendingRed },
    blueTimedOut: timedOut,
    order,
    idealOrder: ideal,
    alerts,
    points,
    mistakes,
    preliminary,
    multiplier,
    cascade,
    extracted: extractedNow,
    heatGained,
    disruption,
    mainContained: alerts.find((a) => a.source === 'main').contained,
  };

  const next = {
    ...s,
    history: [...s.history, entry],
    lastResolution: entry,
    extracted: s.extracted + extractedNow,
    compromise,
    disruption: s.disruption + disruption,
    triagePoints: s.triagePoints + points,
    priorityMistakes: s.priorityMistakes + mistakes,
    deadline: null,
    turn: null,
  };
  if (stageIdx === RULES.rounds - 1) {
    return { ...next, status: 'complete', finishedAt: now, outcome: outcomeFor(next.extracted) };
  }
  return { ...next, status: 'resolved' };
}

/* ------------------------------------------------------- grades & reports */

export function outcomeFor(pct) {
  if (pct <= 0) return { code: 'contained', title: 'THE DATA WAS PROTECTED' };
  if (pct >= 100) return { code: 'full', title: 'COMPLETE DATA LOSS' };
  return { code: 'partial', title: pct < 60 ? 'PARTIAL DATA BREACH' : 'MAJOR DATA BREACH' };
}

export function blueGrade(s) {
  if (s.extracted === 0 && s.triagePoints === RULES.rounds * 3 && s.priorityMistakes === 0) return 'S';
  const p = s.extracted * 1.4 + s.disruption / 500 + s.priorityMistakes * 7;
  if (p <= 12) return 'A';
  if (p <= 32) return 'B';
  if (p <= 65) return 'C';
  if (p <= 95) return 'D';
  return 'F';
}

export function redGrade(s) {
  const x = s.extracted;
  if (x >= 70) return 'S';
  if (x >= 40) return 'A';
  if (x >= 20) return 'B';
  if (x >= 6) return 'C';
  if (x >= 1) return 'D';
  return 'F';
}

export function financialReport(s) {
  const compromised = Math.round(RULES.datasetValueOmr * s.extracted / 100);
  return {
    dataset: RULES.datasetValueOmr,
    compromised,
    protected: RULES.datasetValueOmr - compromised,
    disruption: s.disruption,
    combined: compromised + s.disruption,
  };
}

export function caseStudy(s) {
  const [dropbox, mgm] = CASE_STUDIES;
  if (s.extracted > 35) {
    return { ...mgm, lesson: 'A successful social-engineering attack can disrupt a whole business. Fast, correct triage limits the damage.' };
  }
  if (s.extracted === 0) {
    return { ...dropbox, lesson: 'Even well-defended companies get phished. Your team stopped it here: real attackers only need one missed alert.' };
  }
  return { ...dropbox, lesson: 'One convincing phishing message was enough for real attackers to copy data. Small delays add up.' };
}

/** What a role is allowed to see. Blue never gets hidden priority, truth or which card is Red's. */
export function viewFor(s, role) {
  const base = {
    code: s.code, status: s.status, stage: s.stage, turn: s.turn, deadline: s.deadline,
    history: s.history, lastResolution: s.lastResolution,
    extracted: s.extracted, triagePoints: s.triagePoints, priorityMistakes: s.priorityMistakes,
    finishedAt: s.finishedAt, outcome: s.outcome,
  };
  if (role === 'red') {
    return { ...base, pendingRed: s.pendingRed, redPressure: s.redPressure, compromise: s.compromise };
  }
  if (role === 'blue') {
    return {
      ...base,
      disruption: s.disruption,
      signals: s.status === 'blue'
        ? s.signals.map(({ id, icon, title, desc, severity, clue }) => ({ id, icon, title, desc, severity, clue }))
        : [],
    };
  }
  return base;
}

/** Plain-text summary for the "Copy incident summary" button. */
export function summaryText(s) {
  const f = financialReport(s);
  const omr = (n) => 'OMR ' + n.toLocaleString('en-US');
  const lines = [
    `INCIDENT ATTACK · match ${s.code}`,
    `Outcome: ${s.outcome?.title ?? '—'} (${s.extracted}% of the fictional dataset extracted)`,
    `Red grade: ${redGrade(s)} · Blue grade: ${blueGrade(s)}`,
    `Triage: ${s.triagePoints}/12 · Priority mistakes: ${s.priorityMistakes}`,
    `Data value exposed: ${omr(f.compromised)} · protected: ${omr(f.protected)}`,
    `Response disruption: ${omr(f.disruption)} · Combined impact: ${omr(f.combined)}`,
    '',
    ...s.history.map((h) => `Round ${h.stage + 1} ${STAGES[h.stage].name}: ${h.points}/3 triage, ${h.extracted}% extracted${h.mainContained ? ', main attack contained' : ', main attack handled late'}`),
    '',
    'All figures are illustrative training values, not real company, LNG or criminal-market amounts.',
  ];
  return lines.join('\n');
}
