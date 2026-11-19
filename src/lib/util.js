import { createElement } from 'react';

export const money = (m) => '$' + Math.max(0, m).toFixed(1) + 'M';
export const fmtTime = (s) => `${Math.floor(s / 60)}:${String(Math.round(s % 60)).padStart(2, '0')}`;
export const shuffle = (a) => {
  a = [...a];
  for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; }
  return a;
};

/** Renders trusted, authored HTML from the scenario files (brief / explain / insight). */
export const Html = ({ html, as = 'div', className }) =>
  html ? createElement(as, { className, dangerouslySetInnerHTML: { __html: html } }) : null;

/** Full-screen colour flash + screen shake. Imperative on purpose: pure decoration. */
export function fx(kind) {
  const f = document.getElementById('flash');
  if (f) { f.className = ''; void f.offsetWidth; f.className = 'flash-' + kind; }
  if (kind === 'bad') {
    const a = document.getElementById('app');
    if (a) { a.classList.remove('shake'); void a.offsetWidth; a.classList.add('shake'); }
  }
}

export function setTheme(t) { document.body.className = t ? 'theme-' + t : ''; }
