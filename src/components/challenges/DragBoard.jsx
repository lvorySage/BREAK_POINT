import { useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Sound } from '../../lib/sound.js';
import { isSingleSlot, moveItem, wantZone } from '../../lib/challenge.js';

/**
 * Drag-and-drop board for "sort into buckets" and "put in order" turns.
 * Pointer events cover mouse, touch screens and pens. Tapping a card then a box also works
 * (handy on big touch displays where dragging is awkward).
 */
export default function DragBoard({ ch, value, setValue, locked }) {
  const single = isSingleSlot(ch);
  const labels = single ? ch.items.map((_, i) => String(i + 1)) : ch.buckets;
  const textOf = (id) => (single ? ch.items[id] : ch.items[id].text);
  const zoneOf = (id) => value.place.find((p) => p.id === id)?.z ?? -1;
  const poolIds = value.pool.filter((id) => zoneOf(id) < 0);

  const [sel, setSel] = useState(null);
  const [ghost, setGhost] = useState(null);
  const [hover, setHover] = useState(null);
  const drag = useRef(null);
  const boardRef = useRef(null);

  const move = (id, z) => { Sound.click(); setValue((v) => moveItem(v, id, z, single)); };
  const zoneAt = (x, y) => {
    const el = document.elementFromPoint(x, y)?.closest('[data-zone]');
    return el && boardRef.current?.contains(el) ? +el.dataset.zone : null;
  };

  const tap = (id) => {
    if (sel === id) { setSel(null); return; }
    // a card is already picked up and you tap a card in another box → move/swap into that box
    if (sel !== null && zoneOf(id) >= 0 && zoneOf(id) !== zoneOf(sel)) { move(sel, zoneOf(id)); setSel(null); return; }
    setSel(id); Sound.click();
  };

  const onDown = (e, id) => {
    if (locked) return;
    e.preventDefault();
    const r = e.currentTarget.getBoundingClientRect();
    drag.current = { id, sx: e.clientX, sy: e.clientY, ox: e.clientX - r.left, oy: e.clientY - r.top, w: r.width, moved: false };
    try { e.currentTarget.setPointerCapture(e.pointerId); } catch { /* ignore */ }
  };
  const onMove = (e) => {
    const d = drag.current; if (!d) return;
    if (!d.moved) {
      if (Math.hypot(e.clientX - d.sx, e.clientY - d.sy) < 6) return;
      d.moved = true; setSel(null);
    }
    setGhost({ id: d.id, x: e.clientX - d.ox, y: e.clientY - d.oy, w: d.w });
    setHover(zoneAt(e.clientX, e.clientY));
  };
  const onUp = (e) => {
    const d = drag.current; drag.current = null; if (!d) return;
    if (d.moved) {
      setGhost(null); setHover(null);
      if (e.type === 'pointerup') { const z = zoneAt(e.clientX, e.clientY); if (z !== null) move(d.id, z); }
    } else if (e.type === 'pointerup') tap(d.id);
  };
  const onZoneClick = (e, z) => {
    if (locked || sel === null || e.target.closest('.chip')) return;
    move(sel, z); setSel(null);
  };

  const chip = (id) => {
    let cls = 'chip';
    if (sel === id) cls += ' sel';
    if (ghost?.id === id) cls += ' dragging';
    let fix = null;
    if (locked) {
      const ok = zoneOf(id) === wantZone(ch, id);
      cls += ok ? ' ok' : ' bad';
      if (!ok) fix = <small className="fix">→ {single ? `should be step ${id + 1}` : `belongs in: ${ch.buckets[wantZone(ch, id)]}`}</small>;
    }
    return (
      <div key={id} className={cls} onPointerDown={(e) => onDown(e, id)} onPointerMove={onMove} onPointerUp={onUp} onPointerCancel={onUp}>
        {textOf(id)}{fix}
      </div>
    );
  };

  const dropCls = (z, base) => base + ' drop' + (hover === z ? ' hover' : '');

  return (
    <div ref={boardRef} className={'board ' + (single ? 'board-order' : 'board-sort') + (locked ? ' locked' : '') + (sel !== null ? ' picking' : '')}>
      <div className={dropCls(-1, 'pool')} data-zone={-1} onClick={(e) => onZoneClick(e, -1)}>
        {poolIds.map(chip)}
      </div>
      <div className="zones">
        {labels.map((label, z) => (
          <div key={z} className={dropCls(z, 'zone')} data-zone={z} onClick={(e) => onZoneClick(e, z)}>
            <div className="zone-label">{label}</div>
            <div className="zone-body">{value.place.filter((p) => p.z === z).map((p) => chip(p.id))}</div>
          </div>
        ))}
      </div>
      {ghost && createPortal(
        <div className="chip ghost" style={{ width: ghost.w, transform: `translate(${ghost.x}px, ${ghost.y}px) rotate(-2deg)` }}>{textOf(ghost.id)}</div>,
        document.body,
      )}
    </div>
  );
}
