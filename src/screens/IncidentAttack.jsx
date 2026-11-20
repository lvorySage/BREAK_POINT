import { useCallback, useEffect, useRef, useState } from 'react';
import {
  STAGES, RULES, createMatch, startMatch, rematch, redAct, blueAct, tick, nextRound, financialReport,
} from '../incident/engine.js';
import { RedPane, BluePane } from '../incident/Panes.jsx';
import { RedCharacter, BlueCharacter, RED_SKINS, BLUE_SKINS } from '../incident/Characters.jsx';
import { Finale, reducedMotion } from '../incident/Effects.jsx';
import ResultsView from '../incident/ResultsView.jsx';
import { Sound } from '../lib/sound.js';
import { fx, setTheme } from '../lib/util.js';

const ROADMAP = ['Send phishing email', 'Get account access', 'Find and take files', 'Steal more data'];
const AUTO_NEXT_SECONDS = 20;
const NO_SEL = { primary: null, side: null };
const omr = (n) => 'OMR ' + Math.round(n).toLocaleString('en-US');

/** Position of `el` relative to `arena`, in px. */
function rel(el, arena) {
  const a = arena.getBoundingClientRect(), r = el.getBoundingClientRect();
  return { left: r.left - a.left, top: r.top - a.top, width: r.width, height: r.height };
}

/* --------------------------------------------------------------- lobby */

function RoleCard({ team, skins, skin, setSkin, ready, setReady }) {
  const red = team === 'red';
  const Char = red ? RedCharacter : BlueCharacter;
  return (
    <div className={'ia-role ' + team + (ready ? ' is-ready' : '')}>
      <Char state={ready ? 'typing' : 'idle'} skin={skin} size={150} />
      <h3 className={red ? 'r' : 'b'}>{red ? '🗡️ RED TEAM · Attacker' : '🛡️ BLUE TEAM · SOC analyst'}</h3>
      <p>{red
        ? 'Build a phishing campaign. Each round, pick 1 main attack + 1 distraction, then launch.'
        : 'Three alerts arrive each round. Tap them in order of urgency. Real threats first!'}</p>
      <div className="ia-skins" role="group" aria-label="Character look (cosmetic)">
        {skins.map((s) => (
          <button key={s.id} type="button" className={'chip' + (s.id === skin ? ' on' : '')} aria-pressed={s.id === skin}
            onClick={() => { Sound.click(); setSkin(s.id); }}>{s.name}</button>
        ))}
      </div>
      <button type="button" className={'btn ia-ready ' + team} aria-pressed={ready} onClick={() => { Sound.rank(ready ? 1 : 3); setReady(!ready); }}>
        {ready ? '✔ READY' : 'Tap when ready'}
      </button>
    </div>
  );
}

function Lobby({ code, skins, setSkins, ready, setReady, onStart, onExit, muted, onToggleMute }) {
  const both = ready.red && ready.blue;
  return (
    <main className="screen">
      <section className="ia-lobby">
        <p className="kicker">Security Week · Phishing campaign · Match {code}</p>
        <h1 className="logo ia-logo" aria-label="Incident Attack"><span className="logo-red">INCIDENT</span><span className="logo-blue">ATTACK</span></h1>
        <p className="tagline">Red Team vs Blue Team. Every delay lets more data slip away.</p>
        <div className="ia-roles">
          <RoleCard team="red" skins={RED_SKINS} skin={skins.red} setSkin={(v) => setSkins({ ...skins, red: v })} ready={ready.red} setReady={(v) => setReady({ ...ready, red: v })} />
          <RoleCard team="blue" skins={BLUE_SKINS} skin={skins.blue} setSkin={(v) => setSkins({ ...skins, blue: v })} ready={ready.blue} setReady={(v) => setReady({ ...ready, blue: v })} />
        </div>
        <div className="ia-how">
          <div><b>4 rounds</b>Always all four, even if Blue stops everything early.</div>
          <div><b>{RULES.turnSeconds} seconds</b>per turn, for each team.</div>
          <div><b>Rounds 1–2: planning</b>Red builds access. No data can be stolen yet.</div>
          <div><b>Rounds 3–4: attack</b>Every alert Blue handles late can leak data.</div>
        </div>
        <div className="actions center">
          <button type="button" className="btn ghost" onClick={onExit}>← Back</button>
          <button type="button" className="btn ghost" aria-pressed={muted} onClick={onToggleMute}>{muted ? '🔇 Sound off' : '🔊 Sound on'}</button>
          <button type="button" className="btn primary big" disabled={!both} onClick={onStart}>
            {both ? 'Start match ▶' : 'Both teams tap Ready'}
          </button>
        </div>
        <p className="disclaimer">Simulation only: no emails are sent, no passwords are captured, nothing is attacked. All scenarios and values are fictional.</p>
      </section>
    </main>
  );
}

/* ---------------------------------------------------------- match HUD */

function MatchHud({ g, now, resolvedAt, onNext, onQuit }) {
  const st = STAGES[g.stage];
  const attack = g.stage >= RULES.extractionStartsAtStage;
  const auto = resolvedAt ? Math.max(0, Math.ceil(AUTO_NEXT_SECONDS - (now - resolvedAt) / 1000)) : 0;
  return (
    <div className="ia-hud">
      <div className="ia-hud-top">
        <button type="button" className="icon-btn" title="Quit to menu" onClick={onQuit}>⌂</button>
        <div className="ia-hud-round">
          <b>Round {g.stage + 1}/4 · {st.name}</b>
          <small>{st.subtitle}</small>
        </div>
        <span className={'ia-phase ' + (attack ? 'attack' : 'planning')}>{st.phase}</span>
        {g.status === 'resolved' && (
          <button type="button" className="btn primary ia-next" onClick={onNext}>Next round ▶ <small>({auto})</small></button>
        )}
      </div>
      <div className="ia-hud-bottom">
        <ol className="ia-roadmap" aria-label="Red campaign progress">
          {ROADMAP.map((step, i) => {
            const state = i < g.stage || (i === g.stage && g.status === 'complete') ? 'completed' : i === g.stage ? 'current' : 'locked';
            return <li key={step} className={state}><span>{state === 'completed' ? '✔' : state === 'locked' ? '🔒' : i + 1}</span>{step}</li>;
          })}
        </ol>
        {attack ? (
          <div className="ia-data" aria-label={`${g.extracted}% data extracted`}>
            <div className="ia-data-label"><span>Confirmed data extracted</span><b>{g.extracted}% · {omr(financialReport(g).compromised)}</b></div>
            <div className="ia-data-track"><div className="ia-data-fill" style={{ width: g.extracted + '%' }} /></div>
          </div>
        ) : (
          <div className="ia-data planning">🔒 Red is building access. No data can be stolen yet.</div>
        )}
      </div>
    </div>
  );
}

/* --------------------------------------------------------------- main */

export default function IncidentAttack({ onExit, muted, onToggleMute }) {
  const [view, setView] = useState('lobby'); // lobby | match | finale | results
  const [g, setG] = useState(() => createMatch());
  const [skins, setSkins] = useState({ red: RED_SKINS[0].id, blue: BLUE_SKINS[0].id });
  const [ready, setReady] = useState({ red: false, blue: false });
  const [sel, setSel] = useState(NO_SEL);
  const [rank, setRank] = useState([]);
  const [now, setNow] = useState(() => Date.now());
  const [resolvedAt, setResolvedAt] = useState(null);
  const [comet, setComet] = useState(null);
  const [shield, setShield] = useState(null);
  const [impact, setImpact] = useState(false);
  const arenaRef = useRef(null), redRef = useRef(null), blueRef = useRef(null), bluePanelRef = useRef(null);

  useEffect(() => { setTheme('versus'); }, []);
  useEffect(() => { window.scrollTo(0, 0); }, [view]);

  // Clock: drives the countdown and lets the engine apply timeouts.
  const lastSec = useRef(null);
  const gRef = useRef(g);
  gRef.current = g;
  useEffect(() => {
    if (view !== 'match') return undefined;
    const id = setInterval(() => {
      const t = Date.now();
      const dl = gRef.current.deadline;
      if (dl) {
        const s = Math.ceil((dl - t) / 1000);
        if (s !== lastSec.current && s <= 5 && s > 0) Sound.tick();
        lastSec.current = s;
      }
      setNow(t);
      setG((prev) => tick(prev, t));
    }, 200);
    return () => clearInterval(id);
  }, [view]);

  const advance = useCallback(() => {
    setG((prev) => (prev.status === 'resolved' ? nextRound(prev, Date.now()) : prev));
  }, []);

  // Auto-advance from the round report.
  useEffect(() => {
    if (g.status !== 'resolved') { setResolvedAt(null); return undefined; }
    setResolvedAt(Date.now());
    const id = setTimeout(advance, AUTO_NEXT_SECONDS * 1000);
    return () => clearTimeout(id);
  }, [g.status, g.stage, advance]);

  // Effects on state transitions (cosmetic only).
  // Timers live until unmount, so a quick click can't leave an effect stuck on screen.
  const timers = useRef([]);
  useEffect(() => () => timers.current.forEach(clearTimeout), []);
  const later = (ms, f) => timers.current.push(setTimeout(f, ms));
  const prev = useRef(g);
  useEffect(() => {
    const p = prev.current;
    prev.current = g;
    if (p === g || p.status === g.status) return;

    if (p.status === 'red' && g.status === 'blue') {
      setSel(NO_SEL);
      Sound.launch();
      const arena = arenaRef.current;
      if (!reducedMotion() && arena && redRef.current && blueRef.current) {
        const a = rel(redRef.current, arena), b = rel(blueRef.current, arena);
        const x0 = a.left + a.width / 2, y0 = a.top + Math.min(a.height / 2, 260);
        const x1 = b.left + b.width / 2, y1 = b.top + Math.min(b.height / 2, 260);
        setComet({ key: Date.now(), x0, y0, dx: x1 - x0, dy: y1 - y0 });
        later(650, () => { setImpact(true); Sound.incoming(); });
        later(1100, () => { setComet(null); setImpact(false); });
      } else {
        later(300, () => Sound.incoming());
      }
    }

    if (p.status === 'blue' && (g.status === 'resolved' || g.status === 'complete')) {
      setRank([]);
      Sound.power();
      const arena = arenaRef.current;
      if (!reducedMotion() && arena && blueRef.current) {
        const r = rel(blueRef.current, arena);
        const inset = 6;
        setShield({ key: Date.now(), left: r.left + inset, top: r.top + inset, width: r.width - inset * 2, height: r.height - inset * 2 });
        later(1500, () => setShield(null));
      }
      const res = g.lastResolution;
      later(900, () => {
        if (res.extracted > 0) { Sound.breach(); fx('bad'); }
        else if (res.mainContained) { Sound.contained(); fx('good'); later(350, () => Sound.siren()); }
        else { Sound.bad(); fx('bad'); }
      });
      if (g.status === 'complete') later(2800, () => setView('finale'));
    }
  }, [g]); // eslint-disable-line react-hooks/exhaustive-deps

  const act = (f) => { try { setG(f(g)); } catch { /* buttons only enable valid actions */ } };
  const launch = () => act((s) => redAct(s, sel, Date.now()));
  const submit = () => act((s) => blueAct(s, { order: rank }, Date.now()));
  const toggleRank = (id) => {
    setRank((r) => {
      if (r.includes(id)) { Sound.click(); return r.filter((x) => x !== id); }
      if (r.length >= 3) return r;
      Sound.rank(r.length + 1);
      return [...r, id];
    });
  };
  const pickRed = (s) => { Sound.click(); setSel(s); };
  const start = () => { Sound.power(); setG((s) => startMatch(s, Date.now())); setView('match'); };
  const again = () => { Sound.power(); setG((s) => rematch(s, Date.now())); setSel(NO_SEL); setRank([]); setView('match'); };

  if (view === 'lobby') {
    return <Lobby code={g.code} skins={skins} setSkins={setSkins} ready={ready} setReady={setReady}
      onStart={start} onExit={onExit} muted={muted} onToggleMute={onToggleMute} />;
  }
  if (view === 'results') {
    return <main className="screen"><ResultsView g={g} skins={skins} onRematch={again} onExit={onExit} /></main>;
  }

  const left = g.deadline ? Math.max(0, (g.deadline - now) / 1000) : RULES.turnSeconds;
  return (
    <>
      <MatchHud g={g} now={now} resolvedAt={resolvedAt} onNext={() => { Sound.click(); advance(); }} onQuit={onExit} />
      <main className="ia-main">
        <div className="ia-arena" ref={arenaRef}>
          <RedPane ref={redRef} g={g} skin={skins.red} sel={sel} setSel={pickRed} onLaunch={launch} left={left} />
          <div className={'ia-blue-wrap' + (impact ? ' impact' : '')}>
            <BluePane ref={blueRef} panelRef={bluePanelRef} g={g} skin={skins.blue} rank={rank} toggleRank={toggleRank} onSubmit={submit} left={left} />
          </div>
          {comet && (
            <div key={comet.key} className="ia-comet" aria-hidden="true"
              style={{ left: comet.x0, top: comet.y0, '--dx': comet.dx + 'px', '--dy': comet.dy + 'px' }} />
          )}
          {shield && (
            <div key={shield.key} className="ia-shield" aria-hidden="true"
              style={{ left: shield.left, top: shield.top, width: shield.width, height: shield.height }}>
              <span className="ia-shield-emblem">🛡️</span>
            </div>
          )}
        </div>
      </main>
      {view === 'finale' && (
        <Finale compromised={financialReport(g).compromised} extracted={g.extracted} onDone={() => setView('results')} />
      )}
    </>
  );
}
