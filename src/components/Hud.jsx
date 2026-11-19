import { useEffect, useState } from 'react';
import { useTimerState } from '../lib/timer.js';

/** Video-game style health/loot bar. `float` = { key, text, cls } pops a "-$12M" above the bar. */
export function Meter({ kind, label, value, max, text, level, float }) {
  const [shown, setShown] = useState(false); // start empty, then fill on mount
  useEffect(() => { const t = setTimeout(() => setShown(true), 60); return () => clearTimeout(t); }, []);
  const p = Math.max(0, Math.min(100, max ? (value / max) * 100 : 0));
  const w = (shown ? p : 0) + '%';
  return (
    <div className={'meter ' + kind} data-level={level ?? (p < 25 ? 'low' : p < 60 ? 'mid' : 'high')}>
      <div className="meter-label"><span>{label}</span><b className="meter-val">{text}</b></div>
      <div className="meter-track"><div className="meter-ghost" style={{ width: w }} /><div className="meter-fill" style={{ width: w }} /></div>
      {float && <div key={float.key} className={'float ' + float.cls}>{float.text}</div>}
    </div>
  );
}

function TimerRing() {
  const { left, total, visible } = useTimerState();
  if (!visible) return null;
  return (
    <div className={'timer' + (left <= 10 ? ' low' : '')}>
      <svg viewBox="0 0 36 36">
        <circle className="track" cx="18" cy="18" r="15.9155" />
        <circle className="prog" cx="18" cy="18" r="15.9155" style={{ strokeDashoffset: 100 * (1 - left / total) }} />
      </svg>
      <span className="timer-num">{Math.ceil(left)}</span>
    </div>
  );
}

const PILL = { blue: '🛡️ BLUE TEAM · SOC', red: '🗡️ RED TEAM · SAND VIPER', versus: '⚔️ RED vs BLUE' };

export default function Hud({ mode, step, children, onQuit }) {
  const [armed, setArmed] = useState(false);
  useEffect(() => { if (!armed) return undefined; const t = setTimeout(() => setArmed(false), 3000); return () => clearTimeout(t); }, [armed]);
  return (
    <header className={'hud hud-' + mode}>
      <div className="hud-left"><span className={'team-pill ' + mode}>{PILL[mode]}</span><span className="hud-step">{step}</span></div>
      <div className="hud-bars">{children}</div>
      <div className="hud-right">
        <TimerRing />
        <button type="button" className={'icon-btn' + (armed ? ' armed' : '')} title="Quit to main menu"
          onClick={() => (armed ? onQuit() : setArmed(true))}>{armed ? 'Quit?' : '✕'}</button>
      </div>
    </header>
  );
}
