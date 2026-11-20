import { useState } from 'react';
import { STAGES, blueGrade, redGrade, financialReport, caseStudy, summaryText } from './engine.js';
import { RedCharacter, BlueCharacter } from './Characters.jsx';

const omr = (n) => 'OMR ' + Math.round(n).toLocaleString('en-US');

async function copy(text) {
  try { await navigator.clipboard.writeText(text); return true; } catch { /* fall back below */ }
  try {
    const t = document.createElement('textarea');
    t.value = text; t.style.position = 'fixed'; t.style.opacity = '0';
    document.body.appendChild(t); t.select();
    const ok = document.execCommand('copy');
    t.remove();
    return ok;
  } catch { return false; }
}

export default function ResultsView({ g, skins, onRematch, onExit }) {
  const [copied, setCopied] = useState(null);
  const rg = redGrade(g), bg = blueGrade(g);
  const f = financialReport(g);
  const cs = caseStudy(g);

  return (
    <section className={'ia-results outcome-' + g.outcome.code}>
      <p className="kicker">Incident report · match {g.code}</p>
      <h2 className="ia-outcome">{g.outcome.title}</h2>
      <p className="ia-outcome-sub"><b>{g.extracted}%</b> of the fictional dataset was extracted.</p>

      <div className="ia-grades">
        <div className="ia-grade red">
          <RedCharacter skin={skins.red} size={110} />
          <div><span>Red team grade</span><b className={'grade g-' + rg}>{rg}</b><small>Data extracted: {g.extracted}%</small></div>
        </div>
        <div className="ia-grade blue">
          <BlueCharacter skin={skins.blue} size={110} />
          <div><span>Blue team grade</span><b className={'grade g-' + bg}>{bg}</b><small>Triage {g.triagePoints}/12 · {g.priorityMistakes} priority mistake{g.priorityMistakes === 1 ? '' : 's'}</small></div>
        </div>
      </div>

      <div className="ia-stats">
        <div><span>Data value exposed</span><b className="neg">{omr(f.compromised)}</b></div>
        <div><span>Data value protected</span><b className="pos-g">{omr(f.protected)}</b></div>
        <div><span>Triage score</span><b>{g.triagePoints}/12</b></div>
        <div><span>Priority mistakes</span><b>{g.priorityMistakes}</b></div>
        <div><span>Response disruption</span><b>{omr(f.disruption)}</b></div>
        <div><span>Combined impact</span><b>{omr(f.combined)}</b></div>
      </div>
      <p className="ia-disclaimer">All figures are illustrative training values on a fictional {omr(f.dataset)} dataset. They are not real company assets, LNG prices, criminal profit or measured business losses.</p>

      <div className="panel ia-rounds">
        <h3>Round by round</h3>
        <ol>
          {g.history.map((h) => {
            const st = STAGES[h.stage];
            const main = st.main.find((c) => c.id === h.red.primary);
            const side = st.side.find((c) => c.id === h.red.side);
            return (
              <li key={h.stage}>
                <div className="ia-round-head"><b>Round {h.stage + 1} · {st.name}</b><span className="ia-phase-tag">{st.phase}</span></div>
                <p>Red sent <b>{main.icon} {main.title}</b> with <b>{side.icon} {side.title}</b> as a distraction.
                  {' '}Blue scored <b>{h.points}/3</b>; the main attack was {h.mainContained ? <b className="good">contained in time</b> : <b className="bad">handled late</b>}.
                  {' '}{h.stage >= 2 ? <>Data lost: <b>{h.extracted}%</b>.</> : <>No data could be taken yet.</>}
                  {h.cascade && <b className="bad"> Cascade: all remaining data lost.</b>}</p>
              </li>
            );
          })}
        </ol>
      </div>

      <div className="panel ia-case">
        <p className="kicker">Real-world case study</p>
        <h3>{cs.incident}</h3>
        <p>{cs.detail}</p>
        <p className="ia-lesson">💡 {cs.lesson}</p>
        <p className="ia-hint">Source: <a href={cs.url} target="_blank" rel="noreferrer noopener">{cs.source}</a>. This is a real incident that differs from the game. It is not a value estimate for this scenario.</p>
      </div>

      <div className="actions center">
        <button type="button" className="btn ghost" onClick={onExit}>⌂ Main menu</button>
        <button type="button" className="btn ghost" onClick={async () => setCopied(await copy(summaryText(g)))}>
          {copied === true ? '✔ Copied' : copied === false ? 'Copy failed' : '📋 Copy incident summary'}
        </button>
        <button type="button" className="btn primary big" onClick={onRematch}>↻ Rematch</button>
      </div>
    </section>
  );
}
