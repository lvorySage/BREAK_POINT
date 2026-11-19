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
};
