import { Sound } from '../../lib/sound.js';

/** Multiple choice (single answer) or "select all that apply" (multi). */
export default function ChoiceList({ ch, value, setValue, locked, multi }) {
  const correct = multi ? ch.answers : [ch.answer];
  const isSel = (i) => (multi ? value.sel.includes(i) : value.picked === i);

  const toggle = (i) => {
    Sound.click();
    setValue((v) => (multi
      ? { ...v, sel: v.sel.includes(i) ? v.sel.filter((x) => x !== i) : [...v.sel, i] }
      : { ...v, picked: i }));
  };

  return (
    <div className={'opts' + (multi ? ' multi' : '')}>
      {value.order.map((i, k) => {
        const gone = !multi && value.gone.includes(i);
        const sel = isSel(i);
        let cls = 'opt';
        if (sel) cls += ' sel';
        if (gone) cls += ' gone';
        if (locked) {
          if (correct.includes(i)) cls += sel || !multi ? ' ok' : ' missed';
          else if (sel) cls += ' bad';
        }
        return (
          <button key={i} type="button" className={cls} disabled={locked || gone} onClick={() => toggle(i)}>
            <span className="key">{multi ? '' : 'ABCDEFG'[k]}</span>
            <span className="opt-t">{ch.options[i]}</span>
          </button>
        );
      })}
    </div>
  );
}
