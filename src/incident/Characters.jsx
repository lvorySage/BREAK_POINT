/*
 * Stylised team characters (fictional, not real people). Pure SVG + CSS, so the
 * offline single-file build needs no GIF assets.
 * state: 'idle' (subtle breathing) | 'typing' (arms move, screen flickers).
 * Skins are cosmetic only.
 */

export const RED_SKINS = [
  { id: 'masked', name: 'Masked hacker' },
  { id: 'shadow', name: 'Shadow thief' },
];
export const BLUE_SKINS = [
  { id: 'analyst', name: 'Omani SOC analyst' },
  { id: 'night', name: 'Night-shift analyst' },
];

export function RedCharacter({ state = 'idle', skin = 'masked', size = 150 }) {
  const shadow = skin === 'shadow';
  const hood = shadow ? '#1a1024' : '#130c10';
  const trim = shadow ? '#8b5cf6' : '#fb3b5c';
  return (
    <svg className={`char char-red ${state} skin-${skin}`} viewBox="0 0 160 140" width={size} height={size * 0.875} role="img"
      aria-label={shadow ? 'Shadow thief character' : 'Masked hooded hacker character'}>
      <g className="char-body">
        {/* hoodie body */}
        <path d="M34 140 C36 104 52 90 80 90 C108 90 124 104 126 140 Z" fill={hood} stroke={trim} strokeWidth="1.5" />
        <path d="M80 92 L80 140" stroke={trim} strokeOpacity=".35" strokeWidth="1.5" />
        {/* hood */}
        <path d="M46 70 C44 34 62 16 80 16 C98 16 116 34 114 70 C112 88 98 98 80 98 C62 98 48 88 46 70 Z" fill={hood} stroke={trim} strokeWidth="2" />
        {/* face in shadow */}
        <ellipse cx="80" cy="62" rx="22" ry="25" fill="#07050a" />
        {shadow ? (
          <>
            <path d="M58 66 C66 74 94 74 102 66 L100 84 C92 90 68 90 60 84 Z" fill="#2b1840" />
            <rect className="eyes" x="66" y="54" width="10" height="4" rx="2" fill="#c4b5fd" />
            <rect className="eyes" x="84" y="54" width="10" height="4" rx="2" fill="#c4b5fd" />
          </>
        ) : (
          <>
            <path d="M60 50 C66 44 94 44 100 50 L98 62 C90 66 70 66 62 62 Z" fill="#1d1d24" stroke="#fb3b5c" strokeWidth="1" />
            <rect className="eyes" x="65" y="53" width="11" height="4" rx="2" fill="#ff4d6d" />
            <rect className="eyes" x="84" y="53" width="11" height="4" rx="2" fill="#ff4d6d" />
            <path d="M70 76 L90 76" stroke="#3a1a22" strokeWidth="2" strokeLinecap="round" />
          </>
        )}
      </g>
      {/* desk + laptop */}
      <rect x="18" y="118" width="124" height="22" rx="3" fill="#1b1216" />
      <g className="char-arm arm-l"><path d="M46 112 C40 116 46 122 58 120" stroke={hood} strokeWidth="10" strokeLinecap="round" fill="none" /></g>
      <g className="char-arm arm-r"><path d="M114 112 C120 116 114 122 102 120" stroke={hood} strokeWidth="10" strokeLinecap="round" fill="none" /></g>
      <path d="M50 118 L110 118 L104 96 L56 96 Z" fill="#26191d" stroke={trim} strokeWidth="1.2" />
      <circle cx="80" cy="107" r="3" fill={trim} className="char-screen" />
    </svg>
  );
}

export function BlueCharacter({ state = 'idle', skin = 'analyst', size = 150 }) {
  const night = skin === 'night';
  const robe = night ? '#cfd8e6' : '#f4f6fa';
  return (
    <svg className={`char char-blue ${state} skin-${skin}`} viewBox="0 0 160 140" width={size} height={size * 0.875} role="img"
      aria-label={night ? 'Night-shift SOC analyst character' : 'Omani SOC analyst character wearing a kumma and dishdasha'}>
      <g className="char-body">
        {/* dishdasha */}
        <path d="M36 140 C38 106 54 92 80 92 C106 92 122 106 124 140 Z" fill={robe} stroke="#9fb3cc" strokeWidth="1.2" />
        <path d="M74 94 L80 104 L86 94" fill="none" stroke="#9fb3cc" strokeWidth="1.4" />
        {/* farrukha tassel */}
        <path d="M80 104 L80 122" stroke="#b9c7da" strokeWidth="2" />
        <ellipse cx="80" cy="124" rx="2.6" ry="4" fill="#b9c7da" />
        {/* neck + face */}
        <rect x="72" y="80" width="16" height="14" rx="5" fill="#b07a52" />
        <ellipse cx="80" cy="62" rx="21" ry="24" fill="#c68a5e" />
        {/* beard */}
        <path d="M60 64 C62 84 72 90 80 90 C88 90 98 84 100 64 C96 76 88 80 80 80 C72 80 64 76 60 64 Z" fill="#2a1c14" />
        <path d="M72 74 C76 72 84 72 88 74" stroke="#2a1c14" strokeWidth="3" strokeLinecap="round" fill="none" />
        {/* eyes */}
        <ellipse className="eyes" cx="72" cy="60" rx="2.6" ry="3" fill="#1a120c" />
        <ellipse className="eyes" cx="88" cy="60" rx="2.6" ry="3" fill="#1a120c" />
        <path d="M67 54 L76 53 M84 53 L93 54" stroke="#2a1c14" strokeWidth="2" strokeLinecap="round" />
        {/* kumma (embroidered cap) */}
        <path d="M58 46 C58 28 102 28 102 46 C98 42 62 42 58 46 Z" fill="#ffffff" stroke="#9fb3cc" strokeWidth="1" />
        <path d="M60 44 C64 40 96 40 100 44" stroke="#38bdf8" strokeWidth="1.6" strokeDasharray="2 2.5" fill="none" />
        <path d="M64 37 C70 33 90 33 96 37" stroke={night ? '#64748b' : '#22c55e'} strokeWidth="1.4" strokeDasharray="1.5 2" fill="none" />
        <circle cx="80" cy="33" r="1.6" fill="#38bdf8" />
        {night && (
          <>
            <path d="M57 58 C55 36 105 36 103 58" stroke="#334155" strokeWidth="4" fill="none" />
            <rect x="52" y="54" width="8" height="13" rx="3" fill="#334155" />
            <path d="M56 66 C58 78 66 80 72 80" stroke="#334155" strokeWidth="2" fill="none" />
          </>
        )}
      </g>
      {/* desk + monitor */}
      <rect x="18" y="118" width="124" height="22" rx="3" fill="#0f1a2c" />
      <g className="char-arm arm-l"><path d="M48 112 C42 116 48 122 60 120" stroke={robe} strokeWidth="10" strokeLinecap="round" fill="none" /></g>
      <g className="char-arm arm-r"><path d="M112 112 C118 116 112 122 100 120" stroke={robe} strokeWidth="10" strokeLinecap="round" fill="none" /></g>
      <rect x="54" y="98" width="52" height="20" rx="2" fill="#0b2236" stroke="#38bdf8" strokeWidth="1.2" />
      <g className="char-screen" stroke="#38bdf8" strokeWidth="1.4">
        <path d="M59 104 H80 M59 109 H92 M59 114 H72" />
      </g>
      {night && <rect x="122" y="108" width="9" height="10" rx="2" fill="#7c5a3c" />}
    </svg>
  );
}
