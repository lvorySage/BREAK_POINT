/* Synthesised sound effects — no audio files needed. */
export const Sound = {
  ctx: null,
  muted: false,
  ensure() {
    if (!this.ctx) {
      try { this.ctx = new (window.AudioContext || window.webkitAudioContext)(); } catch { return null; }
    }
    if (this.ctx.state === 'suspended') this.ctx.resume();
    return this.ctx;
  },
  tone(f, d = 0.12, type = 'square', vol = 0.05, delay = 0, f2) {
    if (this.muted) return;
    const c = this.ensure(); if (!c) return;
    const t = c.currentTime + delay, o = c.createOscillator(), g = c.createGain();
    o.type = type; o.frequency.setValueAtTime(f, t);
    if (f2) o.frequency.exponentialRampToValueAtTime(f2, t + d);
    g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.0001, t + d);
    o.connect(g).connect(c.destination); o.start(t); o.stop(t + d + 0.02);
  },
  click() { this.tone(660, 0.05, 'square', 0.03); },
  good() { [523, 659, 784].forEach((f, i) => this.tone(f, 0.15, 'triangle', 0.08, i * 0.09)); },
  mid() { [523, 440].forEach((f, i) => this.tone(f, 0.15, 'triangle', 0.07, i * 0.1)); },
  bad() { this.tone(220, 0.4, 'sawtooth', 0.06, 0, 70); },
  tick() { this.tone(1300, 0.03, 'square', 0.02); },
  power() { this.tone(300, 0.3, 'sine', 0.06, 0, 1200); },
  alarm() { [0, 0.3, 0.6].forEach((d) => this.tone(880, 0.22, 'sawtooth', 0.05, d, 440)); },
  win() { [523, 659, 784, 1047].forEach((f, i) => this.tone(f, 0.25, 'triangle', 0.08, i * 0.13)); },
  // INCIDENT ATTACK effects
  rank(n = 1) { this.tone(500 + n * 140, 0.08, 'triangle', 0.07); },
  launch() { this.tone(180, 0.55, 'sawtooth', 0.06, 0, 900); this.tone(90, 0.5, 'square', 0.03, 0.05, 300); },
  incoming() { [0, 0.16, 0.32].forEach((d) => this.tone(1180, 0.1, 'square', 0.04, d)); },
  breach() { this.tone(160, 0.7, 'sawtooth', 0.07, 0, 50); this.tone(240, 0.5, 'square', 0.03, 0.1, 80); },
  contained() { [392, 523, 659].forEach((f, i) => this.tone(f, 0.18, 'sine', 0.08, i * 0.07)); },
  siren() { [0, 0.35, 0.7, 1.05].forEach((d, i) => this.tone(i % 2 ? 620 : 880, 0.32, 'sawtooth', 0.035, d, i % 2 ? 880 : 620)); },
  final() { [392, 523, 659, 784, 1047].forEach((f, i) => this.tone(f, 0.3, 'triangle', 0.07, i * 0.11)); },
  cash() { this.tone(1568, 0.08, 'square', 0.05); this.tone(2093, 0.35, 'triangle', 0.07, 0.07); },
  decode() { for (let i = 0; i < 8; i++) this.tone(900 + Math.random() * 900, 0.04, 'square', 0.02, i * 0.06); },
};
