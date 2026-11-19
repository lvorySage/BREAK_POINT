import { useEffect, useState } from 'react';
import { LbTable } from '../components/Results.jsx';
import { LB } from '../lib/leaderboard.js';
import { setTheme } from '../lib/util.js';

export default function Leaderboard({ onExit }) {
  const [data, setData] = useState(() => LB.load());
  const [armed, setArmed] = useState(false);

  useEffect(() => {
    setTheme('');
    const onKey = (e) => { if (e.key === 'Escape') onExit(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onExit]);
  useEffect(() => { if (!armed) return undefined; const t = setTimeout(() => setArmed(false), 4000); return () => clearTimeout(t); }, [armed]);

  const reset = () => {
    if (!armed) { setArmed(true); return; }
    LB.reset(); setData(LB.load()); setArmed(false);
  };

  return (
    <main className="screen">
      <section className="lb-screen">
        <h2>🏆 Leaderboard</h2>
        <div className="end-grid">
          <div className="panel"><h3 className="b">🛡️ Blue Team: most money saved</h3><LbTable mode="blue" list={data.blue} /></div>
          <div className="panel"><h3 className="r">🗡️ Red Team: most plunder</h3><LbTable mode="red" list={data.red} /></div>
        </div>
        <div className="actions center">
          <button type="button" className={'btn ghost small' + (armed ? ' armed' : '')} onClick={reset}>
            {armed ? 'Click again to erase all scores' : 'Reset leaderboard'}
          </button>
          <button type="button" className="btn primary big" onClick={onExit}>⌂ Main menu</button>
        </div>
      </section>
    </main>
  );
}
