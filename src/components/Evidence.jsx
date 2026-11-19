/** Realistic-looking artefact: email, EDR alert or log excerpt. */
export default function Evidence({ ev }) {
  return (
    <div className={'evidence ev-' + (ev.kind || 'log')}>
      <div className="ev-title"><span className="ev-dot" />{ev.title}</div>
      {ev.header && (
        <dl>{ev.header.map(([k, v]) => [<dt key={k + 't'}>{k}</dt>, <dd key={k + 'd'}>{v}</dd>])}</dl>
      )}
      {ev.lines && <pre>{ev.lines.join('\n')}</pre>}
    </div>
  );
}
