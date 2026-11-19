import { useEffect, useMemo, useState } from 'react';
import DECK from '../../shared-assets/versus-deck.js';
import { CONFIG } from '../config.js';
import Hud, { Meter } from '../components/Hud.jsx';
import Challenge from '../components/Challenge.jsx';
import Feedback from '../components/Feedback.jsx';
import { CountUp, Debrief } from '../components/Results.jsx';
import { Sound } from '../lib/sound.js';
import { fx, money, setTheme } from '../lib/util.js';

const THEME = { intro: 'versus', pick: 'red', red: 'red', handoff: 'blue', blue: 'blue', end: 'versus' };

/** Two players, one screen. Red picks + executes an attack, Blue answers the resulting alert. */
export default function Versus({ onExit, onRestart }) {
  const phases = DECK.phases;
  const exposure = useMemo(() => phases.reduce((s, p) => s + Math.max(...p.cards.map((c) => c.value)), 0), [phases]);
  const [stage, setStage] = useState('intro');
  const [round, setRound] = useState(0);
  const [card, setCard] = useState(null);
  const [picking, setPicking] = useState(null);
  const [tot, setTot] = useState({ plundered: 0, saved: 0, blocked: 0, launched: 0, log: [] });
  const [fb, setFb] = useState(null);
  const [float, setFloat] = useState(null);
  const p = phases[round];
  const last = round === phases.length - 1;

  useEffect(() => { setTheme(THEME[stage]); window.scrollTo(0, 0); if (stage === 'handoff') Sound.alarm(); if (stage === 'end') Sound.win(); }, [stage]);
  useEffect(() => {
    if (stage !== 'intro' && stage !== 'handoff') return undefined;
    const onKey = (e) => { if (e.key === 'Enter') setStage(stage === 'intro' ? 'pick' : 'blue'); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [stage]);

  const go = (s) => { setFb(null); setStage(s); };
  const nextRound = () => {
    setFb(null); setCard(null); setPicking(null); setFloat(null);
    if (last) setStage('end'); else { setRound((r) => r + 1); setStage('pick'); }
  };
  const pick = (c) => {
    if (picking) return;
    setPicking(c.name); Sound.power();
    setTimeout(() => { setCard(c); setStage('red'); }, 450);
  };

  const onRed = (score, timedOut) => {
    if (score >= 1) {
      setTot((t) => ({ ...t, launched: t.launched + card.value }));
      Sound.good(); fx('good');
      setFb({ cls: 'good', verdict: '✔ Attack launched!', money: <><b className="pos">{money(card.value)}</b> is now at risk. Can the SOC stop it?</>,
        explain: card.red.explain, nextLabel: 'Hand over to Blue Team →', onNext: () => go('handoff') });
    } else {
      setTot((t) => ({ ...t, saved: t.saved + card.value, log: [...t.log, { title: card.name, phase: p.name, score: 1, delta: 'Fizzled', cls: 'pos' }] }));
      Sound.bad(); fx('bad');
      setFb({ cls: 'bad', verdict: (timedOut ? '⏱ Too slow! ' : '✖ ') + 'The attack fizzled.', money: <>{CONFIG.org} keeps <b className="pos">{money(card.value)}</b>.</>,
        explain: card.red.explain, nextLabel: last ? 'Final results →' : 'Next round →', onNext: nextRound });
    }
  };

  const onBlue = (score, timedOut) => {
    const ok = score >= 1;
    setTot((t) => ok
      ? { ...t, saved: t.saved + card.value, blocked: t.blocked + card.value, log: [...t.log, { title: card.name, phase: p.name, score: 1, delta: `Blocked · ${money(card.value)} saved`, cls: 'pos' }] }
      : { ...t, plundered: t.plundered + card.value, log: [...t.log, { title: card.name, phase: p.name, score: 0, delta: `Breach · −${money(card.value)}`, cls: 'neg' }] });
    if (ok) { Sound.good(); fx('good'); } else { Sound.bad(); fx('bad'); setFloat({ key: Date.now(), text: `−${money(card.value)}`, cls: 'neg' }); }
    setFb({
      cls: ok ? 'good' : 'bad',
      verdict: ok ? '🛡️ Attack blocked!' : (timedOut ? '⏱ Too slow! ' : '') + '💥 Breach!',
      money: ok ? <>The SOC saved {CONFIG.org} <b className="pos">{money(card.value)}</b>.</> : <>Red team plundered <b className="neg">{money(card.value)}</b>.</>,
      explain: card.blue.explain, nextLabel: last ? 'Final results →' : 'Next round →', onNext: nextRound,
    });
  };

  if (stage === 'intro') {
    return (
      <main className="screen">
        <section className="briefing versus">
          <p className="kicker">⚔️ Versus mode · 2 players</p>
          <h2>Red vs Blue: {CONFIG.org}</h2>
          <div className="story">Two teams, one screen, {phases.length} rounds. Each round follows one stage of a real attack.</div>
          <div className="vs-steps">
            <div className="r"><b>1 · Red picks an attack card</b>Bigger cards are worth more plunder. Blue team, look away!</div>
            <div className="r"><b>2 · Red executes it</b>Answer the attacker&apos;s question correctly, or the attack fizzles.</div>
            <div className="b"><b>3 · Blue gets the alert</b>Respond correctly to block it and save the money. Miss it and Red takes the loot.</div>
          </div>
          <div className="stakes">
            <div><span>{CONFIG.org} money in play</span><b>{money(exposure)}</b></div>
            <div><span>Rounds</span><b>{phases.length}</b></div>
            <div><span>Seconds per answer</span><b>{CONFIG.versusSeconds}</b></div>
          </div>
          <div className="actions center">
            <button type="button" className="btn ghost" onClick={onExit}>← Back</button>
            <button type="button" className="btn primary big" onClick={() => { Sound.power(); setStage('pick'); }}>Start battle ▶</button>
          </div>
        </section>
      </main>
    );
  }

  const hud = (
    <Hud mode="versus" onQuit={onExit} step={`Round ${Math.min(round + 1, phases.length)} of ${phases.length}`}>
      <Meter kind="blue" label={`${CONFIG.org} money saved`} value={exposure - tot.plundered} max={exposure}
        text={`${money(exposure - tot.plundered)} / ${money(exposure)}`} float={float} />
      <Meter kind="red" label="Red team plunder" value={tot.plundered} max={exposure} text={money(tot.plundered)} />
    </Hud>
  );

  const meta = (extra) => (
    <div className="turn-meta"><span>Round {round + 1}/{phases.length}</span><span className="phase">{p.name}</span>{extra}</div>
  );

  let body;
  if (stage === 'pick') {
    body = (
      <section className="turn">
        <div className="banner red">🗡️ RED TEAM&apos;S MOVE <small>Blue team: look away from the screen!</small></div>
        {meta()}
        <h2>Choose your attack</h2><p className="brief">{p.brief}</p>
        <div className="cards">
          {p.cards.map((c) => (
            <button key={c.name} type="button" className={'attack-card' + (picking === c.name ? ' picked' : '')} onClick={() => pick(c)}>
              <span className="ac-value">{money(c.value)}</span><span className="ac-name">{c.name}</span>
              <span className="ac-desc">{c.desc}</span><span className="ac-risk">{c.risk}</span>
            </button>
          ))}
        </div>
      </section>
    );
  } else if (stage === 'red') {
    body = (
      <section className="turn" key={'r' + round}>
        {meta(<span className="stake">Card value: <b>{money(card.value)}</b></span>)}
        <h2>Execute: {card.name}</h2>
        <p className="brief">{card.desc} Answer correctly to launch the attack.</p>
        <Challenge key={'r' + round} ch={card.red} seconds={CONFIG.versusSeconds} onResolve={onRed} />
        {fb && <Feedback key={'rf' + round} {...fb} />}
      </section>
    );
  } else if (stage === 'handoff') {
    body = (
      <section className="handoff">
        <div className="banner blue">🛡️ BLUE TEAM, YOU&apos;RE UP</div>
        <p className="big-msg">Red team: hands off, step back.<br />An alert just fired in the {CONFIG.org} SOC…</p>
        <button type="button" className="btn primary big" onClick={() => setStage('blue')}>🚨 Open the alert</button>
      </section>
    );
  } else if (stage === 'blue') {
    body = (
      <section className="turn" key={'b' + round}>
        {meta(<span className="stake">At stake: <b>{money(card.value)}</b></span>)}
        <h2>🚨 Incoming alert</h2>
        <p className="brief">Something is happening on the network. Respond correctly to block the attack.</p>
        <Challenge key={'b' + round} ch={card.blue} seconds={CONFIG.versusSeconds} onResolve={onBlue} />
        {fb && <Feedback key={'bf' + round} {...fb} />}
      </section>
    );
  } else {
    const blueWins = tot.plundered <= tot.saved;
    body = (
      <section className="end versus">
        <p className="rank">{blueWins ? '🛡️ Blue Team wins' : '🗡️ Red Team wins'}</p>
        <h2>{blueWins ? 'The SOC held the line' : 'The attackers cashed out'}</h2>
        <div className="vs-score">
          <div className="b"><span>{CONFIG.org} money saved</span><b><CountUp to={tot.saved} /></b></div>
          <div className="vs-x">vs</div>
          <div className="r"><span>Red team plunder</span><b><CountUp to={tot.plundered} /></b></div>
        </div>
        <p className="moral">Red launched attacks worth <b>{money(tot.launched)}</b>. Without a SOC, every one of them lands.
          This SOC blocked <b className="pos">{money(tot.blocked)}</b> worth of attacks.</p>
        <div className="panel narrow"><h3>Round by round</h3><Debrief log={tot.log} /></div>
        <div className="actions center">
          <button type="button" className="btn ghost" onClick={onExit}>⌂ Main menu</button>
          <button type="button" className="btn primary big" onClick={onRestart}>↻ Rematch</button>
        </div>
      </section>
    );
  }

  return <>{hud}<main className="screen">{body}</main></>;
}
