import { forwardRef } from 'react';
import { STAGES, RULES } from './engine.js';
import { RedCharacter, BlueCharacter } from './Characters.jsx';
import { MatrixRain, ScrambleStatus, Siren } from './Effects.jsx';

export const TRUTH = {
  stop: { label: 'Real threat', cls: 'stop' },
  verify: { label: 'Needed checking', cls: 'verify' },
  leave: { label: 'Harmless / approved', cls: 'leave' },
};
const SOURCE = { main: 'Red’s main attack', side: 'Red’s distraction', ambient: 'Background event' };

export function Timer({ left, total, team }) {
  const frac = Math.max(0, Math.min(1, left / total));
  const urgent = left <= 10;
  return (
    <div className={`ia-timer ${team}${urgent ? ' urgent' : ''}`} role="timer" aria-label={`${Math.ceil(left)} seconds left`}>
      <svg viewBox="0 0 40 40"><circle cx="20" cy="20" r="17" className="ring-bg" />
        <circle cx="20" cy="20" r="17" className="ring" style={{ strokeDasharray: 106.8, strokeDashoffset: 106.8 * (1 - frac) }} /></svg>
      <b>{Math.ceil(left)}</b>
    </div>
  );
}

function ChoiceCard({ c, picked, onPick, attackPhase }) {
  return (
    <button type="button" className={'ia-choice' + (picked ? ' picked' : '')} aria-pressed={picked} onClick={onPick}>
      <span className="ia-choice-icon">{c.icon}</span>
      <span className="ia-choice-title">{c.title}</span>
      <span className="ia-choice-desc">{c.desc}</span>
      <span className="ia-choice-tags">
        <span className={'sev ' + c.severity.toLowerCase()}>{c.severity}</span>
        {attackPhase && c.leak > 0 && <span className="tag-loot">Up to {c.leak}% data</span>}
      </span>
    </button>
  );
}

/* ------------------------------------------------------------------ RED */

export const RedPane = forwardRef(function RedPane({ g, skin, sel, setSel, onLaunch, left }, ref) {
  const st = STAGES[g.stage];
  const attackPhase = g.stage >= RULES.extractionStartsAtStage;
  const acting = g.status === 'red';
  const stopped = g.history.filter((h) => h.mainContained).length;
  const res = g.status === 'resolved' || g.status === 'complete' ? g.lastResolution : null;

  let body;
  if (acting) {
    body = (
      <>
        <p className="ia-prompt">1 · {st.mainPrompt}</p>
        <p className="ia-hint">{st.mainHint}</p>
        <div className="ia-choices">
          {st.main.map((c) => <ChoiceCard key={c.id} c={c} attackPhase={attackPhase} picked={sel.primary === c.id} onPick={() => setSel({ ...sel, primary: c.id })} />)}
        </div>
        <p className="ia-prompt">2 · {st.sidePrompt}</p>
        <p className="ia-hint">{st.sideHint}</p>
        <div className="ia-choices">
          {st.side.map((c) => <ChoiceCard key={c.id} c={c} attackPhase={attackPhase} picked={sel.side === c.id} onPick={() => setSel({ ...sel, side: c.id })} />)}
        </div>
        <button type="button" className="btn ia-launch" disabled={!sel.primary || !sel.side} onClick={onLaunch}>
          🚀 LAUNCH ATTACK
        </button>
        <p className="ia-hint center">{!sel.primary || !sel.side ? 'Pick one attack and one distraction.' : 'Ready.'} If time runs out, the first two cards are used.</p>
      </>
    );
  } else if (g.status === 'blue') {
    body = (
      <div className="ia-wait">
        <p className="ia-wait-big">🔒 Attack launched</p>
        <p>Your two picks are hidden so Blue can&apos;t peek.</p>
        <p className="ia-hint">The SOC is sorting its alerts. Hands off. Results appear after Blue submits.</p>
      </div>
    );
  } else if (res) {
    const main = st.main.find((c) => c.id === res.red.primary);
    const side = st.side.find((c) => c.id === res.red.side);
    const mainA = res.alerts.find((a) => a.source === 'main');
    const sideA = res.alerts.find((a) => a.source === 'side');
    const line = (c, a) => (
      <li className={a.correct === 'stop' ? (a.contained ? 'bad' : 'good') : 'muted-row'}>
        <span>{c.icon} {c.title}</span>
        <b>{a.correct !== 'stop' ? 'Noise only' : a.contained ? 'STOPPED' : attackPhase ? (a.leak ? `GOT THROUGH · +${a.leak}% data` : 'GOT THROUGH') : 'GOT THROUGH · access gained'}</b>
      </li>
    );
    body = (
      <div className="ia-result">
        <p className={'ia-result-head ' + (res.mainContained ? 'bad' : 'good')}>
          {res.mainContained ? '🚨 Your main attack was stopped' : '😈 Your main attack slipped through'}
        </p>
        <ul className="ia-result-list">{line(main, mainA)}{line(side, sideA)}</ul>
        {attackPhase
          ? <p className="ia-result-sum">Data taken this round: <b>{res.extracted}%</b>{res.multiplier !== 1 && <> (final push ×{res.multiplier.toFixed(2)})</>}</p>
          : <p className="ia-result-sum">Access built this round: <b>+{res.heatGained}</b>. No data can be taken until the attack phase.</p>}
        {res.red.timedOut && <p className="ia-hint">⏱ Time ran out, so the first cards were used.</p>}
        {res.cascade && <p className="ia-cascade">💥 CASCADE: every delay added up. All reachable data is gone.</p>}
      </div>
    );
  }

  return (
    <section ref={ref} className={'ia-pane red' + (acting ? ' active' : '')} aria-label="Red team: attacker">
      <Siren level={stopped} />
      <header className="ia-pane-head">
        <RedCharacter state={acting ? 'typing' : 'idle'} skin={skin} size={118} />
        <div>
          <p className="ia-team r">RED TEAM · ATTACKER</p>
          <p className="ia-pane-status">{acting ? 'Your move' : g.status === 'blue' ? 'Waiting for the SOC…' : 'Round report'}</p>
          {stopped > 0 && <p className="ia-sirens">🚨 Main attacks stopped: {stopped}</p>}
        </div>
        {acting && <Timer left={left} total={RULES.turnSeconds} team="red" />}
      </header>
      <div className="ia-pane-body">{body}</div>
    </section>
  );
});

/* ----------------------------------------------------------------- BLUE */

export const BluePane = forwardRef(function BluePane({ g, skin, rank, toggleRank, onSubmit, left, panelRef }, ref) {
  const acting = g.status === 'blue';
  const attackPhase = g.stage >= RULES.extractionStartsAtStage;
  const res = g.status === 'resolved' || g.status === 'complete' ? g.lastResolution : null;
  const signals = acting ? g.signals.map(({ id, icon, title, desc, severity, clue }) => ({ id, icon, title, desc, severity, clue })) : [];

  let body;
  if (acting) {
    body = (
      <>
        <ScrambleStatus fraction={1 - left / RULES.turnSeconds} />
        <p className="ia-prompt">Tap the alerts in order: most urgent first.</p>
        <p className="ia-hint">The severity label can mislead. Read the clue.</p>
        <div className="ia-alerts">
          {signals.map((a, i) => {
            const r = rank.indexOf(a.id);
            return (
              <button key={a.id} type="button" className={'ia-alert' + (r >= 0 ? ' ranked' : '')} style={{ animationDelay: i * 120 + 'ms' }}
                aria-pressed={r >= 0} aria-label={`${a.title}. ${a.severity}. ${a.clue}.${r >= 0 ? ` Ranked number ${r + 1}.` : ''}`}
                onClick={() => toggleRank(a.id)}>
                <span className={'sev ' + a.severity.toLowerCase()}>{a.severity}</span>
                <span className="ia-alert-icon">{a.icon}</span>
                <span className="ia-alert-title">{a.title}</span>
                <span className="ia-alert-desc">{a.desc}</span>
                <span className="ia-alert-clue">🔎 {a.clue}</span>
                {r >= 0 && <span className="ia-rank-badge">#{r + 1}</span>}
              </button>
            );
          })}
        </div>
        <button type="button" className="btn ia-submit" disabled={rank.length !== 3} onClick={onSubmit}>
          🛡️ HANDLE IN THIS ORDER
        </button>
        <p className="ia-hint center">{rank.length < 3 ? `Pick #${rank.length + 1}. Tap a numbered card to undo.` : 'Ready.'} If time runs out, cards are handled left to right.</p>
      </>
    );
  } else if (g.status === 'red') {
    body = (
      <div className="ia-wait">
        <p className="ia-wait-big">📡 Watching the network</p>
        <p>The attacker is choosing their next move.</p>
        <p className="ia-hint">Blue team: eyes on your own screen. Three alerts are about to arrive.</p>
      </div>
    );
  } else if (res) {
    const byId = Object.fromEntries(res.alerts.map((a) => [a.id, a]));
    body = (
      <div className="ia-result">
        <p className={'ia-result-head ' + (res.points === 3 ? 'good' : res.extracted ? 'bad' : 'warn')}>
          {res.points === 3 ? '✅ Perfect order' : res.extracted ? `💥 Late response: ${res.extracted}% data lost` : `⚠️ ${res.mistakes} priority mistake${res.mistakes > 1 ? 's' : ''}`}
          <span className="ia-pts">{res.points}/3</span>
        </p>
        <ol className="ia-review">
          {res.alerts.map((a) => (
            <li key={a.id}>
              <span className="ia-review-n">#{a.queue}</span>
              <span className="ia-review-main">
                <b>{a.icon} {a.title}</b>
                <small><span className={'truth ' + TRUTH[a.correct].cls}>{TRUTH[a.correct].label}</span> · {SOURCE[a.source]} · urgency {a.priority}/10</small>
              </span>
              <span className="ia-review-out">
                {a.correct === 'stop'
                  ? (a.contained ? <span className="good">Contained in time</span> : <span className="bad">Handled late{attackPhase && a.leak ? ` · −${a.leak}%` : ''}</span>)
                  : a.disruption ? <span className="warn">Time spent · OMR {a.disruption}</span> : <span className="muted">No cost</span>}
              </span>
            </li>
          ))}
        </ol>
        {res.points < 3 && <p className="ia-hint">Best order: {res.idealOrder.map((id) => byId[id].title).join(' → ')}</p>}
        {res.blueTimedOut && <p className="ia-hint">⏱ Time ran out, so cards were handled left to right.</p>}
      </div>
    );
  }

  return (
    <section ref={ref} className={'ia-pane blue' + (acting ? ' active' : '')} aria-label="Blue team: SOC defender">
      <MatrixRain intensity={g.extracted / 100} />
      <header className="ia-pane-head">
        <BlueCharacter state={acting ? 'typing' : 'idle'} skin={skin} size={118} />
        <div>
          <p className="ia-team b">BLUE TEAM · SOC</p>
          <p className="ia-pane-status">{acting ? 'Alerts incoming. Sort them!' : g.status === 'red' ? 'Standing by…' : 'Round report'}</p>
        </div>
        {acting && <Timer left={left} total={RULES.turnSeconds} team="blue" />}
      </header>
      <div className="ia-pane-body" ref={panelRef}>{body}</div>
    </section>
  );
});
