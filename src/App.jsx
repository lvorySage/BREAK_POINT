import { useCallback, useEffect, useRef, useState } from 'react';
import { CONFIG } from './config.js';
import Title from './screens/Title.jsx';
import Campaign from './screens/Campaign.jsx';
import Versus from './screens/Versus.jsx';
import Leaderboard from './screens/Leaderboard.jsx';
import { Sound } from './lib/sound.js';

export default function App() {
  // `run` changes on every navigation so restarting the same mode remounts fresh state.
  const [route, setRoute] = useState({ name: 'title', run: 0 });
  const [muted, setMuted] = useState(false);
  const go = useCallback((name, extra = {}) => setRoute((r) => ({ name, ...extra, run: r.run + 1 })), []);
  const home = useCallback(() => go('title'), [go]);

  useEffect(() => { window.scrollTo(0, 0); }, [route.run]);

  // Booth mode: nobody touched anything for a while → back to the title screen.
  const routeRef = useRef(route);
  routeRef.current = route;
  useEffect(() => {
    let lastInput = Date.now();
    const bump = () => { lastInput = Date.now(); };
    document.addEventListener('pointerdown', bump, true);
    document.addEventListener('keydown', bump, true);
    const id = setInterval(() => {
      if (routeRef.current.name !== 'title' && Date.now() - lastInput > CONFIG.idleResetSeconds * 1000) go('title');
    }, 5000);
    return () => { clearInterval(id); document.removeEventListener('pointerdown', bump, true); document.removeEventListener('keydown', bump, true); };
  }, [go]);

  const toggleMute = () => { Sound.muted = !muted; setMuted(!muted); };

  let screen;
  switch (route.name) {
    case 'campaign':
      screen = <Campaign key={route.run} mode={route.mode} onExit={home} onRestart={() => go('campaign', { mode: route.mode })} />;
      break;
    case 'versus':
      screen = <Versus key={route.run} onExit={home} onRestart={() => go('versus')} />;
      break;
    case 'leaderboard':
      screen = <Leaderboard key={route.run} onExit={home} />;
      break;
    default:
      screen = <Title key={route.run} onStart={(m) => (m === 'versus' ? go('versus') : go('campaign', { mode: m }))} onLeaderboard={() => go('leaderboard')} />;
  }

  return (
    <>
      <div id="app">{screen}</div>
      <button type="button" id="btn-mute" className="icon-btn" title="Sound on/off" aria-pressed={muted} onClick={toggleMute}>{muted ? '🔇' : '🔊'}</button>
    </>
  );
}
