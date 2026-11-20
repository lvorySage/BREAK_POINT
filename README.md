# Breach Point · Incident Attack

Red Team vs Blue Team cyber-awareness games for a Security Week booth. Runs offline in one browser.

- **Blue Team / Red Team / Versus**: quiz-style campaign modes.
- **Incident Attack**: a 2-player split-screen phishing-campaign game. Red picks an attack and a distraction; Blue sorts three SIEM alerts by urgency. 4 rounds, 30 seconds a turn.

```bash
npm install
npm run dev     # http://localhost:5173
npm test        # game-engine tests
npm run build   # dist/index.html: one self-contained file, double-click to play offline
```

Full guide, controls and content-editing notes: [docs/README.md](docs/README.md).
The Incident Attack design spec is `INCIDENT_ATTACK_Developer_Handoff_V5_1.json`.

All scenarios, companies, people and values are fictional. Simulation only: nothing is sent, captured or attacked.
