import { useEffect, useState } from 'react';
import { fmtTime, money } from '../lib/util.js';

export function CountUp({ to, ms = 1600 }) {
  const [v, setV] = useState(0);
  useEffect(() => {
    const t0 = performance.now(); let raf;
    const step = (t) => { const p = Math.min(1, (t - t0) / ms); setV(to * (1 - Math.pow(1 - p, 3))); if (p < 1) raf = requestAnimationFrame(step); };
    raf = requestAnimationFrame(step);
    return () => cancelAnimationFrame(raf);
  }, [to, ms]);
  return money(v);
}

export function Debrief({ log }) {
  return (
    <ol className="debrief">
      {log.map((l, i) => {
        const state = l.score >= 1 ? 'ok' : l.score <= 0 ? 'bad' : 'mid';
        return (
          <li key={i} className={state}>
            <span className="d-icon">{{ ok: '✔', bad: '✖', mid: '◐' }[state]}</span>
            <span className="d-title">{l.title}<small>{l.phase}</small></span>
            <span className={'d-money ' + l.cls}>{l.delta}</span>
          </li>
        );
      })}
    </ol>
  );
}

export function LbTable({ mode, list, hi = -1 }) {
  return (
    <table className={'lb ' + mode}>
      <thead><tr><th>#</th><th>Name</th><th>{mode === 'blue' ? 'Saved' : 'Plundered'}</th><th>Time</th></tr></thead>
      <tbody>
        {list.length === 0 && <tr><td colSpan={4} className="empty">No scores yet. Be the first!</td></tr>}
        {list.slice(0, 10).map((e, i) => (
          <tr key={i} className={i === hi ? 'me' : ''}><td>{i + 1}</td><td>{e.name}</td><td>{money(e.score)}</td><td>{fmtTime(e.time)}</td></tr>
        ))}
      </tbody>
    </table>
  );
}
