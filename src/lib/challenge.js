import { shuffle } from './util.js';

/* Pure answer-state helpers for the four challenge types.
 *   mcq   : { order, picked, gone }
 *   multi : { order, sel }
 *   sort / order : { pool, place: [{ id, z }] }   (z = zone index; insertion order = display order) */

export const typeOf = (ch) => ch.type || 'mcq';
export const isSingleSlot = (ch) => typeOf(ch) === 'order';

export function initValue(ch) {
  const t = typeOf(ch);
  if (t === 'mcq') return { order: shuffle(ch.options.map((_, i) => i)), picked: null, gone: [] };
  if (t === 'multi') return { order: shuffle(ch.options.map((_, i) => i)), sel: [] };
  return { pool: shuffle(ch.items.map((_, i) => i)), place: [] };
}

export function isReady(ch, v) {
  const t = typeOf(ch);
  if (t === 'mcq') return v.picked !== null;
  if (t === 'multi') return v.sel.length > 0;
  return v.place.length === ch.items.length;
}

export const wantZone = (ch, id) => (isSingleSlot(ch) ? id : ch.items[id].bucket);

/** 0..1. Scores whatever is placed/selected, so a timeout still earns partial credit. */
export function scoreOf(ch, v) {
  const t = typeOf(ch);
  if (t === 'mcq') return v.picked === ch.answer ? 1 : 0;
  if (t === 'multi') {
    let tp = 0, fp = 0;
    v.sel.forEach((i) => (ch.answers.includes(i) ? tp++ : fp++));
    return Math.max(0, (tp - fp) / ch.answers.length); // wrong picks cancel right ones
  }
  return v.place.filter((p) => p.z === wantZone(ch, p.id)).length / ch.items.length;
}

/** Move item `id` to zone `z` (-1 = back to the pool). Single-slot zones swap occupants. */
export function moveItem(v, id, z, single) {
  const from = v.place.find((p) => p.id === id)?.z ?? -1;
  if (from === z) return v;
  let place = v.place.filter((p) => p.id !== id);
  if (single && z >= 0) {
    const occ = place.find((p) => p.z === z);
    if (occ) place = place.filter((p) => p !== occ).concat(from >= 0 ? [{ id: occ.id, z: from }] : []);
  }
  if (z >= 0) place = place.concat({ id, z });
  return { ...v, place };
}

/** 50/50 lifeline: hide all but the right answer and one wrong one. */
export function fiftyFifty(ch, v) {
  const gone = shuffle(v.order.filter((i) => i !== ch.answer)).slice(0, Math.max(0, v.order.length - 2));
  return { ...v, gone, picked: gone.includes(v.picked) ? null : v.picked };
}
