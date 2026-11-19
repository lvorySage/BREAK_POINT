import { useEffect, useMemo, useState } from 'react';
import BLUE from '../../blue-team/scenarios.js';
import RED from '../../red-team/scenarios.js';
import { CONFIG } from '../config.js';
import Hud, { Meter } from '../components/Hud.jsx';
import Challenge from '../components/Challenge.jsx';
import Feedback from '../components/Feedback.jsx';
import { CountUp, Debrief, LbTable } from '../components/Results.jsx';
import { LB } from '../lib/leaderboard.js';
import { Sound } from '../lib/sound.js';
import { Html, fmtTime, fx, money, setTheme } from '../lib/util.js';

const verdictOf = (score, timedOut, [win, lose]) => {
  const pre = timedOut ? '⏱ Time\'s up! ' : '';
  if (score >= 1) return { cls: 'good', text: pre + win };
  if (score <= 0) return { cls: 'bad', text: pre + lose };
  return { cls: 'mid', text: pre + `◐ Partly right: ${Math.round(score * 100)}%` };
};

/** Single-player campaign: briefing → turns → results. mode = 'blue' | 'red'. */
export default function Campaign({ mode, onExit, onRestart }) {
  const data = mode === 'blue' ? BLUE : RED;
  const blue = mode === 'blue';
  const total = useMemo(() => data.turns.reduce((s, t) => s + t.value, 0), [data]);
  const [stage, setStage] = useState('brief'); // brief | play | end
  const [g, setG] = useState({ idx: 0, saved: total, loot: 0, heat: 0, caught: false, lifeline: true, log: [], t0: 0, t1: 0 });
  const [fb, setFb] = useState(null);
  const [floats, setFloats] = useState({});

  useEffect(() => { setTheme(mode); }, [mode]);
  useEffect(() => { window.scrollTo(0, 0); }, [stage, g.idx]);
  useEffect(() => {
    if (stage !== 'brief') return undefined;
    const onKey = (e) => { if (e.key === 'Enter') begin(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  });

  const begin = () => { Sound.power(); setG((s) => ({ ...s, t0: Date.now() })); setStage('play'); };
  const turn = data.turns[g.idx];
  const last = g.idx === data.turns.length - 1;

  const resolve = (score, timedOut) => {
    const t = turn;
    const v = verdictOf(score, timedOut, blue
      ? ['✔ Threat neutralised!', '✖ Missed. The attacker got through.']
      : ['✔ Success! The SOC didn\'t see a thing.', '✖ Busted move. You made noise.']);
    const n = { ...g, log: [...g.log] };
    let moneyLine;
    if (blue) {
      const loss = t.value * (1 - score);
      n.saved -= loss;
      n.log.push({ title: t.title, phase: t.phase, score, delta: loss > 0 ? `−${money(loss)}` : `+${money(t.value)} saved`, cls: loss > 0 ? 'neg' : 'pos' });
      if (loss > 0) setFloats({ saved: { key: Date.now(), text: `−${money(loss)}`, cls: 'neg' } });
      moneyLine = loss <= 0
        ? <>You kept the full <b className="pos">{money(t.value)}</b> safe inside {CONFIG.org}.</>
        : <>{CONFIG.org} lost <b className="neg">−{money(loss)}</b>. You saved {money(t.value - loss)} of {money(t.value)}.</>;
    } else {
      const gain = t.value * score;
      const heat = Math.round((t.heat ?? 34) * (1 - score));
      n.loot += gain; n.heat = Math.min(100, n.heat + heat);
      if (n.heat >= 100) n.caught = true;
      n.log.push({ title: t.title, phase: t.phase, score, delta: gain > 0 ? `+${money(gain)}` : '$0', cls: gain > 0 ? 'pos' : 'neg' });
      setFloats({
        loot: gain > 0 ? { key: Date.now(), text: `+${money(gain)}`, cls: 'pos' } : null,
        heat: heat > 0 ? { key: Date.now() + 1, text: `+${heat}%`, cls: 'neg' } : null,
      });
      moneyLine = <>You plundered <b className="pos">+{money(gain)}</b>{heat
        ? <> · SOC heat <b className="neg">+{heat}%</b> (now {n.heat}%)</> : ' · SOC heat unchanged'}</>;
    }
    if (n.caught || last) n.t1 = Date.now();
    setG(n);

    if (n.caught) { Sound.alarm(); fx('bad'); } else if (v.cls === 'good') { Sound.good(); fx('good'); } else if (v.cls === 'bad') { Sound.bad(); fx('bad'); } else Sound.mid();
    setFb({
      cls: n.caught ? 'bad' : v.cls,
      verdict: n.caught ? '🚨 DETECTED! The SOC has found your operation.' : v.text,
      money: moneyLine, explain: t.explain, insight: t.insight,
      insightLabel: blue ? '💡 Why the SOC matters' : '🛡️ How the SOC counters this',
      nextLabel: n.caught ? 'Face the consequences →' : last ? 'See final results →' : 'Next turn →',
    });
  };

  const next = () => {
    setFb(null); setFloats({});
    if (g.caught || last) setStage('end');
    else setG((s) => ({ ...s, idx: s.idx + 1 }));
  };

  if (stage === 'brief') {
    return (
      <main className="screen">
        <section className={'briefing ' + mode}>
          <p className="kicker">{blue ? '🛡️ Blue Team briefing' : '🗡️ Red Team briefing'}</p>
          <h2>{data.name}</h2>
          <p className="role">Your role: <b>{data.role}</b></p>
          <Html className="story" html={data.intro} />
          <div className="stakes">
            <div><span>{blue ? 'Money at risk tonight' : 'Loot available'}</span><b>{money(total)}</b></div>
            <div><span>Turns</span><b>{data.turns.length}</b></div>
            <div><span>Seconds per turn</span><b>{CONFIG.turnSeconds}</b></div>
          </div>
          <ul className="rules">{data.rules.map((r, i) => <Html as="li" key={i} html={r} />)}</ul>
          <div className="actions center">
            <button type="button" className="btn ghost" onClick={onExit}>← Back</button>
            <button type="button" className="btn primary big" onClick={begin}>{blue ? 'Start shift' : 'Launch operation'} ▶</button>
          </div>
        </section>
      </main>
    );
  }

  const hud = (
    <Hud mode={mode} onQuit={onExit} step={`Turn ${Math.min(g.idx + 1, data.turns.length)} of ${data.turns.length}`}>
      {blue
        ? <Meter kind="blue" label={`Money saved · ${CONFIG.org}`} value={g.saved} max={total} text={`${money(g.saved)} / ${money(total)}`} float={floats.saved} />
        : <>
            <Meter kind="red" label="Plunder" value={g.loot} max={total} text={`${money(g.loot)} / ${money(total)}`} float={floats.loot} />
            <Meter kind="heat" label="SOC heat · detection risk" value={g.heat} max={100} text={`${g.heat}%`}
              level={g.heat >= 66 ? 'low' : g.heat >= 34 ? 'mid' : 'high'} float={floats.heat} />
          </>}
    </Hud>
  );

  if (stage === 'end') return <>{hud}<CampaignEnd mode={mode} g={g} total={total} onExit={onExit} onRestart={onRestart} /></>;

  return (
    <>
      {hud}
      <main className="screen">
        <section className="turn" key={g.idx}>
          <div className="turn-meta">
            <span>Turn {g.idx + 1}/{data.turns.length}</span><span className="phase">{turn.phase}</span>
            <span className="stake">{blue ? 'At stake' : 'Loot on the table'}: <b>{money(turn.value)}</b></span>
          </div>
          <h2>{turn.title}</h2>
          <Html as="p" className="brief" html={turn.brief} />
          <Challenge key={g.idx} ch={turn} seconds={CONFIG.turnSeconds}
            lifelineLabel={g.lifeline ? data.lifeline : null}
            onLifeline={() => setG((s) => ({ ...s, lifeline: false }))}
            onResolve={resolve} />
          {fb && <Feedback {...fb} onNext={next} />}
        </section>
      </main>
    </>
  );
}

function CampaignEnd({ mode, g, total, onExit, onRestart }) {
  const blue = mode === 'blue';
  const secs = Math.round((g.t1 - g.t0) / 1000);
  const [saved, setSaved] = useState(null); // rank index once saved
  const [name, setName] = useState('');

  let score, rank, headline, sub, cls;
  if (blue) {
    score = g.saved; const p = g.saved / total;
    rank = p >= 0.9 ? 'SOC Legend' : p >= 0.75 ? 'Threat Hunter' : p >= 0.5 ? 'Tier-2 Analyst' : p >= 0.25 ? 'Junior Analyst' : 'Breached';
    headline = `You saved ${CONFIG.org}`; cls = 'blue';
    sub = <>out of <b>{money(total)}</b> at risk. The attacker got away with <b className="neg">{money(total - g.saved)}</b>.</>;
  } else if (g.caught) {
    score = 0; rank = 'Busted'; headline = 'Operation over. Loot seized'; cls = 'busted';
    sub = <>You had <b>{money(g.loot)}</b> before the SOC traced you. It&apos;s all frozen now.<br />{CONFIG.org} kept <b className="pos">{money(total)}</b>. <b>That&apos;s what a SOC is for.</b></>;
  } else {
    score = g.loot; const p = g.loot / total;
    rank = p >= 0.8 ? 'Phantom APT' : p >= 0.6 ? 'Elite Operator' : p >= 0.4 ? 'Crime Crew' : 'Script Kiddie';
    headline = 'You plundered'; cls = 'red';
    sub = <>out of <b>{money(total)}</b> on the table. A well-run SOC would have kept <b className="pos">{money(total - g.loot)}</b> safe.</>;
  }

  useEffect(() => {
    setTheme(mode); window.scrollTo(0, 0);
    if (g.caught || (blue && g.saved / total < 0.5)) Sound.alarm(); else Sound.win();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const save = (e) => {
    e.preventDefault();
    const i = LB.add(mode, { name: name.trim() || 'Anonymous', score: Math.round(score * 10) / 10, time: secs, date: Date.now() });
    setSaved(i); Sound.good();
  };

  return (
    <main className="screen">
      <section className={'end ' + cls}>
        <p className="rank">{rank}</p>
        <h2>{headline}</h2>
        <div className="hero-num"><CountUp to={blue ? g.saved : g.caught ? 0 : g.loot} /></div>
        <p className="sub">{sub}</p>
        <p className="moral">{blue
          ? 'Every dollar in that bar is money a real SOC keeps inside the organisation: through detection, fast response and people who report suspicious emails.'
          : 'Attackers need every step to succeed. Defenders only need to catch one. Each “How the SOC counters this” you saw is a chance the SOC has to stop an attack.'}</p>
        <div className="end-grid">
          <div className="panel"><h3>Debrief</h3><Debrief log={g.log} /><p className="muted">Time: {fmtTime(secs)}</p></div>
          <div className="panel"><h3>🏆 Leaderboard</h3>
            {saved === null
              ? <form className="save" onSubmit={save}>
                  <input maxLength={18} placeholder="Your name or callsign" autoComplete="off" required value={name} onChange={(e) => setName(e.target.value)} />
                  <button className="btn primary" type="submit">Save score</button>
                </form>
              : <p className="saved-msg">Saved! You placed <b>#{saved + 1}</b>.</p>}
            <LbTable mode={mode} list={LB.load()[mode]} hi={saved ?? -1} />
          </div>
        </div>
        <div className="actions center">
          <button type="button" className="btn ghost" onClick={onExit}>⌂ Main menu</button>
          <button type="button" className="btn primary big" onClick={onRestart}>↻ Play again</button>
        </div>
      </section>
    </main>
  );
}
