/*
 * Purely cosmetic effects. Nothing in this file may change engine state,
 * money, extracted % or grades — it only displays numbers it is given.
 */
import { useEffect, useRef, useState } from 'react';
import { Sound } from '../lib/sound.js';

export const reducedMotion = () => typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

const GLYPHS = 'アイウエオカキクケコサシスセソタチツテト0123456789ABCDEF<>/{}#$%';
const glyph = () => GLYPHS[Math.floor(Math.random() * GLYPHS.length)];

/**
 * Blue-side data rain. `intensity` 0..1 comes from CONFIRMED extracted % only:
 * more columns and brighter as data is lost. Not a claim that encryption is happening.
 */
export function MatrixRain({ intensity = 0, dense = false }) {
  const ref = useRef(null);
  const level = useRef(intensity);
  level.current = dense ? 1 : intensity;

  useEffect(() => {
    const cv = ref.current; if (!cv) return undefined;
    const ctx = cv.getContext('2d');
    const size = 15;
    let cols = [], raf = 0, last = 0, w = 0, h = 0;
    const slow = reducedMotion();
    const fit = () => {
      w = cv.width = cv.offsetWidth; h = cv.height = cv.offsetHeight;
      cols = Array.from({ length: Math.ceil(w / size) }, () => ({ y: Math.random() * h, speed: 0.6 + Math.random() * 0.9, seed: Math.random() }));
    };
    fit();
    const ro = new ResizeObserver(fit); ro.observe(cv);
    const frame = (t) => {
      raf = requestAnimationFrame(frame);
      if (t - last < (slow ? 260 : 55)) return;
      last = t;
      const lv = level.current;
      ctx.fillStyle = 'rgba(4,10,20,0.18)';
      ctx.fillRect(0, 0, w, h);
      ctx.font = `${size}px monospace`;
      const share = 0.1 + lv * 0.9; // fraction of columns that are raining
      cols.forEach((c, i) => {
        if (c.seed > share) return;
        ctx.fillStyle = lv > 0.5 && i % 3 === 0 ? '#ef4444' : '#22d3ee';
        ctx.fillText(glyph(), i * size, c.y);
        c.y += size * c.speed;
        if (c.y > h + 20) c.y = -Math.random() * 120;
      });
    };
    raf = requestAnimationFrame(frame);
    return () => { cancelAnimationFrame(raf); ro.disconnect(); };
  }, []);

  const opacity = dense ? 0.85 : 0.12 + intensity * 0.6;
  return <canvas ref={ref} className="matrix-rain" style={{ opacity }} aria-hidden="true" />;
}

/**
 * Blue-turn tension effect: the feed label scrambles as the clock runs down.
 * Cosmetic only — it does not affect timing or scoring.
 */
export function ScrambleStatus({ fraction }) {
  const [, force] = useState(0);
  useEffect(() => { const id = setInterval(() => force((n) => n + 1), 140); return () => clearInterval(id); }, []);
  const base = 'SOC LIVE FEED · ALERT QUEUE OPEN';
  const text = [...base].map((ch) => (ch !== ' ' && Math.random() < fraction * 0.85 ? glyph() : ch)).join('');
  const status = fraction < 0.34 ? 'WATCHING LIVE' : fraction < 0.67 ? 'SYSTEM UNDER THREAT' : 'BREACH IN PROGRESS';
  const cls = fraction < 0.34 ? 'ok' : fraction < 0.67 ? 'warn' : 'bad';
  return (
    <div className="scramble" aria-hidden="true">
      <span className="scramble-text">{text}</span>
      <span className={'scramble-status ' + cls}>● {status}</span>
    </div>
  );
}

/** Red-side siren lighting; grows with each main attack the SOC stopped. */
export function Siren({ level }) {
  if (!level) return null;
  return <div className={`siren siren-${Math.min(level, 4)}`} aria-hidden="true" />;
}

/* ------------------------------------------------------------ finale */

const omr = (n) => 'OMR ' + Math.round(n).toLocaleString('en-US');
const T = { revealAt: 3550, tickMs: 70, ticks: 10, doneAt: 5050, loading: 3900 };

/**
 * Cinematic before the results. Red: cash settlement with the true amount hidden
 * until a short random digit roll ends on it. Blue: decrypting incident report.
 */
export function Finale({ compromised, extracted, onDone }) {
  const [shown, setShown] = useState(null); // null = hidden by CSS
  const [settled, setSettled] = useState(false);
  const done = useRef(onDone);
  done.current = onDone;

  useEffect(() => {
    Sound.decode();
    const timers = [];
    const decodeId = setInterval(() => Sound.decode(), 900);
    timers.push(setTimeout(() => {
      clearInterval(decodeId);
      const digits = String(Math.max(compromised, 10000)).length;
      for (let i = 0; i < T.ticks; i++) {
        timers.push(setTimeout(() => setShown(Math.floor(Math.random() * 10 ** digits)), i * T.tickMs));
      }
      timers.push(setTimeout(() => { setShown(compromised); setSettled(true); Sound.cash(); }, T.ticks * T.tickMs));
    }, T.revealAt));
    timers.push(setTimeout(() => { Sound.final(); done.current(); }, T.doneAt));
    return () => { timers.forEach(clearTimeout); clearInterval(decodeId); };
  }, [compromised]);

  return (
    <div className="finale" role="status" aria-live="polite">
      <div className="finale-half red">
        <p className="finale-kicker">RED TEAM · CASH SETTLEMENT</p>
        <div className="finale-load"><span style={{ animationDuration: T.loading + 'ms' }} /></div>
        <p className={'finale-amount' + (shown == null ? ' hidden' : '') + (settled ? ' settled' : '')}>
          {shown == null ? 'OMR 000,000' : omr(shown)}
        </p>
        <p className="finale-note">{settled ? `${extracted}% of the fictional dataset` : 'Counting stolen data…'}</p>
      </div>
      <div className="finale-half blue">
        <MatrixRain dense />
        <div className="finale-blue-text">
          <p className="finale-kicker">BLUE TEAM · SOC</p>
          <p className="finale-decrypt">DECRYPTING INCIDENT REPORT</p>
          <p className="finale-note">Rebuilding the timeline…</p>
        </div>
      </div>
    </div>
  );
}
