import './App.css';
import { useEffect, useRef, useState } from 'react';
import { fetchAbout } from './services/about';
import { fetchForecast } from './services/forecast';

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

function getApiEndpoint(path) {
  const apiBaseUrl = import.meta.env.VITE_WEATHER1116_API_URL?.replace(/\/$/, '');
  return apiBaseUrl ? `${apiBaseUrl}${path}` : path;
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
    metadata.push(`Started ${node.buildStart}`);
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
  const [helloMessage, setHelloMessage] = useState('Loading hello message...');
  const [forecasts, setForecasts] = useState(null);
  const [forecastError, setForecastError] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const [aboutTree, setAboutTree] = useState(null);
  const [aboutError, setAboutError] = useState('');
  const [isAboutLoading, setIsAboutLoading] = useState(false);
  const avatarMenuRef = useRef(null);

  useEffect(() => {
    let isMounted = true;

    fetch(getApiEndpoint('/Home/Hello'))
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Request failed: ${response.status}`);
        }

        return response.json();
      })
      .then((data) => {
        if (isMounted) {
          setHelloMessage(data.requestResponse ?? 'No hello response returned.');
        }
      })
      .catch(() => {
        if (isMounted) {
          setHelloMessage('Unable to load hello message from API.');
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    let isMounted = true;

    fetchForecast()
      .then((data) => {
        if (isMounted) {
          setForecasts(data);
        }
      })
      .catch(() => {
        if (isMounted) {
          setForecastError('Unable to load weather forecast from API.');
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

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

  const handleAboutClick = async () => {
    setIsMenuOpen(false);
    setIsAboutOpen(true);
    setIsAboutLoading(true);
    setAboutError('');

    try {
      const data = await fetchAbout();
      setAboutTree(data);
    } catch {
      setAboutTree(null);
      setAboutError('Unable to load About information.');
    } finally {
      setIsAboutLoading(false);
    }
  };

  const closeAboutModal = () => {
    setIsAboutOpen(false);
  };

  return (
    <div className="app">
      <header className="top-bar">
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
      </header>

      <main className="home-content">
        <p className="hello-message">{helloMessage}</p>

        <h2 className="forecast-title">Weather forecast</h2>

        {!forecasts && !forecastError && <p className="forecast-status">Loading...</p>}
        {forecastError && <p className="forecast-status error">{forecastError}</p>}
        {forecasts && (
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
              {isAboutLoading && <p className="about-status">Loading About information...</p>}
              {!isAboutLoading && aboutError && <p className="about-status error">{aboutError}</p>}
              {!isAboutLoading && !aboutError && aboutTree && (
                <ul className="about-tree-list root">
                  <AboutTreeNode node={aboutTree} />
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
