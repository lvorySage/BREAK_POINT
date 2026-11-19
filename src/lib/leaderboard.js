/* Per-machine leaderboard in localStorage (survives restarts of the booth PC's browser). */
const KEY = 'breachpoint.leaderboard.v1';
const empty = () => ({ blue: [], red: [] });

export const LB = {
  load() {
    try { const d = JSON.parse(localStorage.getItem(KEY)); return d && d.blue && d.red ? d : empty(); } catch { return empty(); }
  },
  save(d) { try { localStorage.setItem(KEY, JSON.stringify(d)); } catch { /* storage blocked: board won't persist */ } },
  /** Adds an entry and returns its 0-based rank. */
  add(mode, entry) {
    const d = this.load();
    d[mode].push(entry);
    d[mode].sort((a, b) => b.score - a.score || a.time - b.time);
    d[mode] = d[mode].slice(0, 100);
    this.save(d);
    return d[mode].indexOf(entry);
  },
  reset() { this.save(empty()); },
};
