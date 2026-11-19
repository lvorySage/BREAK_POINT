import { useEffect } from 'react';
import { CONFIG } from '../config.js';
import { LB } from '../lib/leaderboard.js';
import { money, setTheme } from '../lib/util.js';
import { Sound } from '../lib/sound.js';

function Top3({ list }) {
  if (!list.length) return <ol><li className="empty">No scores yet</li></ol>;
  return <ol>{list.slice(0, 3).map((e, i) => <li key={i}><span>{['🥇', '🥈', '🥉'][i]} {e.name}</span><b>{money(e.score)}</b></li>)}</ol>;
}

export default function Title({ onStart, onLeaderboard }) {
  const lb = LB.load();
  const start = (m) => { Sound.power(); onStart(m); };

  useEffect(() => {
    setTheme('');
    const onKey = (e) => { if (e.key === '1') start('blue'); if (e.key === '2') start('red'); if (e.key === '3') start('versus'); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  });

  const fullscreen = () => {
    const d = document.documentElement;
    if (!document.fullscreenElement) d.requestFullscreen?.(); else document.exitFullscreen?.();
  };

  return (
    <main className="screen">
      <section className="title-screen">
        <p className="event">{CONFIG.org} · {CONFIG.event}</p>
        <h1 className="logo" aria-label="Breach Point"><span className="logo-red">BREACH</span><span className="logo-blue">POINT</span></h1>
        <p className="tagline">A turn-based <b className="r">Red Team</b> vs <b className="b">Blue Team</b> cyber battle. Every decision has a price.</p>
        <div className="modes">
          <button type="button" className="mode blue" onClick={() => start('blue')}>
            <span className="mode-icon">🛡️</span><span className="mode-name">Blue Team</span>
            <span className="mode-sub">Run the SOC through one night. Your health bar is the <b>money you save {CONFIG.org}</b>.</span>
            <span className="mode-cta">Defend →</span>
          </button>
          <button type="button" className="mode red" onClick={() => start('red')}>
            <span className="mode-icon">🗡️</span><span className="mode-name">Red Team</span>
            <span className="mode-sub">Think like the attacker. <b>Plunder</b> as much as you can before the SOC catches you.</span>
            <span className="mode-cta">Attack →</span>
          </button>
          <button type="button" className="mode versus" onClick={() => start('versus')}>
            <span className="mode-icon">⚔️</span><span className="mode-name">Versus · 2 players</span>
            <span className="mode-sub">Red picks the attack, Blue responds to the alert. One screen, two teams.</span>
            <span className="mode-cta">Battle →</span>
          </button>
        </div>
        <div className="mini-lb">
          <div><h3 className="b">🛡️ Top defenders</h3><Top3 list={lb.blue} /></div>
          <div><h3 className="r">🗡️ Top attackers</h3><Top3 list={lb.red} /></div>
        </div>
        <div className="title-foot">
          <button type="button" className="btn ghost" onClick={() => { Sound.click(); onLeaderboard(); }}>🏆 Full leaderboard</button>
          <button type="button" className="btn ghost" onClick={fullscreen}>⛶ Full screen</button>
        </div>
        <p className="disclaimer">All scenarios, people, domains, IPs and amounts are fictional and made for security-awareness training.</p>
      </section>
    </main>
  );
}
