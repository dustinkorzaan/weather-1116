import './App.css';
import { useEffect, useRef, useState } from 'react';
import {
  useGetForecastQuery,
  useGetHelloQuery,
  useLazyGetAboutQuery,
} from './services/weatherApi';

/** Formats an API date-only string (yyyy-MM-dd) in local time, matching .NET ToShortDateString(). */
function formatForecastDate(isoDate) {
  const datePart = String(isoDate).split('T')[0];
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) {
    return datePart;
  }
  return new Date(year, month - 1, day).toLocaleDateString();
}

function kelvinToC(kelvin) {
  return Number.isFinite(kelvin) ? kelvin - 273.15 : NaN;
}

function kelvinToF(kelvin) {
  return Number.isFinite(kelvin) ? ((kelvin - 273.15) * 9) / 5 + 32 : NaN;
}

function formatTemp(value) {
  return Number.isFinite(value) ? value.toFixed(2) : 'N/A';
}

/** Formats a build timestamp like "7/12/2026 10:22:14 PM" (matches MVC and Blazor). */
function formatBuildStart(isoDate) {
  const date = new Date(isoDate);
  if (Number.isNaN(date.getTime())) {
    return String(isoDate);
  }

  const month = date.getUTCMonth() + 1;
  const day = date.getUTCDate();
  const year = date.getUTCFullYear();
  const minutes = String(date.getUTCMinutes()).padStart(2, '0');
  const seconds = String(date.getUTCSeconds()).padStart(2, '0');
  const period = date.getUTCHours() >= 12 ? 'PM' : 'AM';
  const hours = date.getUTCHours() % 12 || 12;

  return `${month}/${day}/${year} ${hours}:${minutes}:${seconds} ${period}`;
}

function AboutTreeNode({ node }) {
  if (!node) {
    return null;
  }

  const hasChildren = Array.isArray(node.children) && node.children.length > 0;
  const metadata = [];
  if (Number.isFinite(node.buildNumber)) {
    metadata.push(`Build #${node.buildNumber}`);
  }
  if (node.buildStart) {
    metadata.push(`Started ${formatBuildStart(node.buildStart)}`);
  }

  return (
    <li className="about-tree-item">
      <div className="about-tree-row">
        <span className="about-tree-name">{node.name ?? 'Unnamed node'}</span>
        <span className={`about-tree-health ${node.isHealthy ? 'healthy' : 'unhealthy'}`}>
          {node.isHealthy ? 'Healthy' : 'Unhealthy'}
        </span>
      </div>
      {metadata.length > 0 && <div className="about-tree-meta">{metadata.join(' | ')}</div>}

      {hasChildren && (
        <ul className="about-tree-list">
          {node.children.map((child, index) => (
            <AboutTreeNode key={`${child.name ?? 'node'}-${index}`} node={child} />
          ))}
        </ul>
      )}
    </li>
  );
}

function App() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const avatarMenuRef = useRef(null);
  const { data: helloMessage, isError: isHelloError } = useGetHelloQuery();
  const {
    data: forecasts,
    isLoading: isForecastLoading,
    isError: isForecastError,
  } = useGetForecastQuery();
  const [loadAbout, aboutQuery] = useLazyGetAboutQuery();

  useEffect(() => {
    if (!isMenuOpen) {
      return undefined;
    }

    const handleClickOutside = (event) => {
      if (avatarMenuRef.current && !avatarMenuRef.current.contains(event.target)) {
        setIsMenuOpen(false);
      }
    };

    const handleKeyDown = (event) => {
      if (event.key === 'Escape') {
        setIsMenuOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isMenuOpen]);

  const handleAvatarClick = () => {
    setIsMenuOpen((open) => !open);
  };

  const handleAboutClick = () => {
    setIsMenuOpen(false);
    setIsAboutOpen(true);
    loadAbout();
  };

  const closeAboutModal = () => {
    setIsAboutOpen(false);
  };

  return (
    <div className="app">
      <header className="top-bar">
        <div className="top-bar-inner">
          <div className="site-brand">
            <img src="/logo.svg" alt="Weather logo" className="site-logo" />
            <h1 className="title">Weather React</h1>
          </div>

          <div className="avatar-menu" ref={avatarMenuRef}>
            <button
              type="button"
              className="avatar-button"
              aria-haspopup="true"
              aria-expanded={isMenuOpen}
              aria-label="Open user menu"
              onClick={handleAvatarClick}
            >
              <svg viewBox="0 0 24 24" className="avatar-icon" aria-hidden="true" focusable="false">
                <circle cx="12" cy="8" r="4" fill="currentColor" />
                <path
                  d="M4 20c0-4.418 3.582-8 8-8s8 3.582 8 8"
                  fill="currentColor"
                />
              </svg>
            </button>

            {isMenuOpen && (
              <ul className="avatar-dropdown" role="menu">
                <li role="none">
                  <button type="button" role="menuitem" className="avatar-dropdown-item" onClick={handleAboutClick}>
                    About
                  </button>
                </li>
              </ul>
            )}
          </div>
        </div>
      </header>

      <main className="home-content">
        <p className="hello-message">
          {isHelloError ? 'Unable to load hello message from API.' : (helloMessage ?? 'Loading hello message...')}
        </p>

        <h2 className="forecast-title">Weather forecast</h2>

        {isForecastLoading && <p className="forecast-status">Loading...</p>}
        {isForecastError && <p className="forecast-status error">Unable to load weather forecast from API.</p>}
        {forecasts && (
          <div className="table-responsive">
            <table className="forecast-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Temp. (C)</th>
                  <th>Temp. (F)</th>
                  <th>Summary</th>
                </tr>
              </thead>
              <tbody>
                {forecasts.map((forecast) => (
                  <tr key={forecast.date}>
                    <td>{formatForecastDate(forecast.date)}</td>
                    <td>{formatTemp(kelvinToC(forecast.temperatureK))}</td>
                    <td>{formatTemp(kelvinToF(forecast.temperatureK))}</td>
                    <td>{forecast.summary}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </main>

      {isAboutOpen && (
        <div className="modal-backdrop" role="presentation" onClick={closeAboutModal}>
          <div
            className="modal-dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="about-modal-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="modal-header">
              <h2 id="about-modal-title" className="modal-title">About</h2>
              <button
                type="button"
                className="modal-close"
                aria-label="Close"
                onClick={closeAboutModal}
              >
                &times;
              </button>
            </div>
            <div className="modal-body">
              {aboutQuery.isFetching && <p className="about-status">Loading About information...</p>}
              {!aboutQuery.isFetching && aboutQuery.isError && (
                <p className="about-status error">Unable to load About information.</p>
              )}
              {!aboutQuery.isFetching && !aboutQuery.isError && aboutQuery.data && (
                <ul className="about-tree-list root">
                  <AboutTreeNode node={aboutQuery.data} />
                </ul>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
