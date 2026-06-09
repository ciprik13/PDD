import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Camera, Leaf, Sparkles, ScanLine, Activity, ClipboardCheck } from 'lucide-react';
import { useDashboardStats } from '@/hooks/useApi';
import styles from './Landing.module.css';

/**
 * Public marketing intro (`variant="public"`, shown at /welcome) and the in-app
 * home (`variant="home"`, shown at / inside the app shell). Same hero + content;
 * the home variant drops the marketing top bar, links CTAs into the app, and
 * shows a live stats strip.
 */
export function LandingPage({ variant = 'public' }: { variant?: 'public' | 'home' }) {
  const { t } = useTranslation();
  const isHome = variant === 'home';

  const features = [
    { icon: <Camera size={18} />, title: t('landing.f1.title'), desc: t('landing.f1.desc') },
    { icon: <Leaf size={18} />, title: t('landing.f2.title'), desc: t('landing.f2.desc') },
    { icon: <Sparkles size={18} />, title: t('landing.f3.title'), desc: t('landing.f3.desc') },
  ];

  const steps = [
    { icon: <ScanLine size={18} />, title: t('landing.s1.title'), desc: t('landing.s1.desc') },
    { icon: <Activity size={18} />, title: t('landing.s2.title'), desc: t('landing.s2.desc') },
    { icon: <ClipboardCheck size={18} />, title: t('landing.s3.title'), desc: t('landing.s3.desc') },
  ];

  return (
    <div className={[styles.page, isHome ? styles.pageHome : ''].join(' ')}>
      {!isHome && (
        <header className={styles.topbar}>
          <div className={styles.brand}>
            <img src="/agricure.png" alt="AgriCure" className={styles.logo} />
            <span className={styles.brandName}>AgriCure</span>
          </div>
          <Link to="/login" className={styles.signInTop}>{t('landing.signIn')}</Link>
        </header>
      )}

      <main className={styles.hero}>
        <span className={styles.eyebrow}>{t('landing.eyebrow')}</span>
        <h1 className={styles.title} dangerouslySetInnerHTML={{ __html: t('landing.title') }} />
        <p className={styles.lede}>{t('landing.lede')}</p>
        <div className={styles.ctaRow}>
          {isHome ? (
            <>
              <Link to="/camera" className={styles.ctaPrimary}>{t('landing.ctaCamera')}</Link>
              <Link to="/plants" className={styles.ctaSecondary}>{t('landing.ctaPlants')}</Link>
            </>
          ) : (
            <Link to="/login" className={styles.ctaPrimary}>{t('landing.cta')}</Link>
          )}
        </div>

        {isHome && <LiveStats />}

        <div className={styles.features}>
          {features.map((f) => (
            <div key={f.title} className={styles.feature}>
              <div className={styles.featureIcon}>{f.icon}</div>
              <div className={styles.featureTitle}>{f.title}</div>
              <div className={styles.featureDesc}>{f.desc}</div>
            </div>
          ))}
        </div>

        {/* How it works */}
        <div className={styles.howWrap}>
          <h2 className={styles.sectionTitle}>{t('landing.howTitle')}</h2>
          <div className={styles.steps}>
            {steps.map((s, i) => (
              <div key={s.title} className={styles.step}>
                <div className={styles.stepNum}>{i + 1}</div>
                <div className={styles.featureIcon}>{s.icon}</div>
                <div className={styles.featureTitle}>{s.title}</div>
                <div className={styles.featureDesc}>{s.desc}</div>
              </div>
            ))}
          </div>
          <p className={styles.diseases}>{t('landing.diseases')}</p>
        </div>
      </main>

      {!isHome && <footer className={styles.footer}>{t('landing.footer')}</footer>}
    </div>
  );
}

function LiveStats() {
  const { t } = useTranslation();
  const { data } = useDashboardStats();
  if (!data) return null;

  const items = [
    { label: t('dashboard.stats.detectionsToday'), value: data.detectionsToday },
    { label: t('dashboard.stats.avgConfidence'), value: `${Math.round(data.avgConfidence * 100)}%` },
    { label: t('dashboard.stats.rowsScanned'), value: `${data.rowsScanned}/${data.totalRows}` },
    { label: t('dashboard.stats.plantsTracked'), value: data.plantsTracked },
  ];

  return (
    <div className={styles.stats}>
      {items.map((s) => (
        <div key={s.label} className={styles.stat}>
          <div className={styles.statValue}>{s.value}</div>
          <div className={styles.statLabel}>{s.label}</div>
        </div>
      ))}
    </div>
  );
}
