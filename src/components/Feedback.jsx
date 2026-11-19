import { useEffect, useRef, useState } from 'react';
import { Html } from '../lib/util.js';
import { Sound } from '../lib/sound.js';

/** Verdict + money impact + explanation shown after each answer. */
export default function Feedback({ cls, verdict, money, explain, insight, insightLabel, nextLabel, onNext }) {
  const [armed, setArmed] = useState(false); // short delay so a double Enter can't skip the lesson
  const ref = useRef(null);
  const next = () => { if (!armed) return; Sound.click(); onNext(); };

  useEffect(() => {
    const t = setTimeout(() => setArmed(true), 700);
    const s = setTimeout(() => ref.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }), 80);
    return () => { clearTimeout(t); clearTimeout(s); };
  }, []);
  useEffect(() => {
    const onKey = (e) => { if (e.key === 'Enter' && armed) { e.preventDefault(); next(); } };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  });

  return (
    <div ref={ref} className={'feedback ' + cls}>
      <div className="fb-verdict">{verdict}</div>
      {money && <div className="fb-money">{money}</div>}
      <Html className="fb-explain" html={explain} />
      {insight && (
        <div className="fb-insight"><b>{insightLabel}</b><Html as="span" html={insight} /></div>
      )}
      <div className="actions">
        <button type="button" className="btn primary big" disabled={!armed} onClick={next}>{nextLabel}</button>
      </div>
    </div>
  );
}
