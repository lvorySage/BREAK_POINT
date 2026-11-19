import { useEffect, useRef, useSyncExternalStore } from 'react';
import { Sound } from './sound.js';

/* Tiny store so the HUD's timer ring can show the countdown owned by the active challenge. */
let state = { left: 0, total: 1, visible: false };
const subs = new Set();
export const timerStore = {
  get: () => state,
  subscribe: (fn) => { subs.add(fn); return () => subs.delete(fn); },
  set(patch) { state = { ...state, ...patch }; subs.forEach((f) => f()); },
};
export const useTimerState = () => useSyncExternalStore(timerStore.subscribe, timerStore.get);

/** Counts down while `running`; calls onExpire once at zero. */
export function useCountdown(seconds, running, onExpire) {
  const cb = useRef(onExpire);
  cb.current = onExpire;
  useEffect(() => {
    if (!running) return undefined;
    const end = Date.now() + seconds * 1000;
    let last = null;
    timerStore.set({ left: seconds, total: seconds, visible: true });
    const id = setInterval(() => {
      const left = Math.max(0, (end - Date.now()) / 1000);
      timerStore.set({ left });
      const s = Math.ceil(left);
      if (s !== last && s <= 10 && s > 0) Sound.tick();
      last = s;
      if (left <= 0) { clearInterval(id); cb.current(); }
    }, 200);
    return () => clearInterval(id);
  }, [running, seconds]);
  useEffect(() => () => timerStore.set({ visible: false }), []);
}
