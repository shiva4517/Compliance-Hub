import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import logo from './assets/ComplianceHub-Logo.png'

document.title = 'Compliance Hub'

const favicon =
  document.querySelector<HTMLLinkElement>("link[rel~='icon']") ??
  document.createElement('link')

favicon.rel = 'icon'
favicon.type = 'image/png'
favicon.href = logo

if (!favicon.parentNode) {
  document.head.appendChild(favicon)
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
