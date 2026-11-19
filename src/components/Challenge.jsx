import { useCallback, useEffect, useRef, useState } from 'react';
import ChoiceList from './challenges/ChoiceList.jsx';
import DragBoard from './challenges/DragBoard.jsx';
import Evidence from './Evidence.jsx';
import { fiftyFifty, initValue, isReady, scoreOf, typeOf } from '../lib/challenge.js';
import { useCountdown } from '../lib/timer.js';
import { Sound } from '../lib/sound.js';

const TYPE_LABEL = {
  mcq: 'Choose one answer',
  multi: 'Select all that apply',
  sort: 'Drag each card into the right box (or tap a card, then a box)',
  order: 'Drag into the correct order (or tap a card, then a slot)',
};

/**
 * One timed question. Owns its answer state; reveals right/wrong after lock-in and
 * reports onResolve(score 0..1, timedOut). Give it a `key` per turn so state resets.
 */
export default function Challenge({ ch, seconds, lifelineLabel, onLifeline, onResolve }) {
  const type = typeOf(ch);
  const [value, setValue] = useState(() => initValue(ch));
  const [locked, setLocked] = useState(false);
  const [lifeUsed, setLifeUsed] = useState(false);
  const valueRef = useRef(value);
  valueRef.current = value;
  const lockedRef = useRef(false);
  const ready = isReady(ch, value);

  const finish = useCallback((timedOut) => {
    if (lockedRef.current) return;
    lockedRef.current = true;
    setLocked(true);
    onResolve(scoreOf(ch, valueRef.current), timedOut);
  }, [ch, onResolve]);

  useCountdown(seconds, !locked, () => finish(true));

  useEffect(() => {
    if (locked) return undefined;
    const onKey = (e) => {
      if (e.target.matches?.('input, textarea')) return;
      if (/^[1-9]$/.test(e.key) && (type === 'mcq' || type === 'multi')) {
        const i = value.order[+e.key - 1];
        if (i === undefined || (type === 'mcq' && value.gone.includes(i))) return;
        Sound.click();
        setValue((v) => (type === 'mcq' ? { ...v, picked: i } : { ...v, sel: v.sel.includes(i) ? v.sel.filter((x) => x !== i) : [...v.sel, i] }));
      } else if (e.key === 'Enter' && ready) { e.preventDefault(); finish(false); }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [locked, ready, type, value, finish]);

  const useFifty = () => {
    setLifeUsed(true); Sound.power();
    setValue((v) => fiftyFifty(ch, v));
    onLifeline?.();
  };

  return (
    <>
      <div className="challenge">
        {ch.evidence && <Evidence ev={ch.evidence} />}
        <div className="prompt"><span className="ptype">{TYPE_LABEL[type]}</span>{ch.prompt}</div>
        {type === 'mcq' && <ChoiceList ch={ch} value={value} setValue={setValue} locked={locked} />}
        {type === 'multi' && <ChoiceList ch={ch} value={value} setValue={setValue} locked={locked} multi />}
        {(type === 'sort' || type === 'order') && <DragBoard ch={ch} value={value} setValue={setValue} locked={locked} />}
      </div>
      {!locked && (
        <div className="actions">
          {lifelineLabel && type === 'mcq' && !lifeUsed && (
            <button type="button" className="btn lifeline" onClick={useFifty}>{lifelineLabel}</button>
          )}
          <button type="button" className="btn primary big" disabled={!ready} onClick={() => finish(false)}>Lock in answer</button>
        </div>
      )}
    </>
  );
}
