import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Camera, Leaf, Sparkles } from 'lucide-react';
import styles from './Landing.module.css';

export function LandingPage() {
  const { t } = useTranslation();

  const features = [
    { icon: <Camera size={18} />, title: t('landing.f1.title'), desc: t('landing.f1.desc') },
    { icon: <Leaf size={18} />, title: t('landing.f2.title'), desc: t('landing.f2.desc') },
    { icon: <Sparkles size={18} />, title: t('landing.f3.title'), desc: t('landing.f3.desc') },
  ];

  return (
    <div className={styles.page}>
      <header className={styles.topbar}>
        <div className={styles.brand}>
          <img src="/agricure.png" alt="AgriCure" className={styles.logo} />
          <span className={styles.brandName}>AgriCure</span>
        </div>
        <Link to="/login" className={styles.signInTop}>{t('landing.signIn')}</Link>
      </header>

      <main className={styles.hero}>
        <span className={styles.eyebrow}>{t('landing.eyebrow')}</span>
        <h1 className={styles.title}
          dangerouslySetInnerHTML={{ __html: t('landing.title') }} />
        <p className={styles.lede}>{t('landing.lede')}</p>
        <div className={styles.ctaRow}>
          <Link to="/login" className={styles.ctaPrimary}>{t('landing.cta')}</Link>
        </div>

        <div className={styles.features}>
          {features.map((f) => (
            <div key={f.title} className={styles.feature}>
              <div className={styles.featureIcon}>{f.icon}</div>
              <div className={styles.featureTitle}>{f.title}</div>
              <div className={styles.featureDesc}>{f.desc}</div>
            </div>
          ))}
        </div>
      </main>

      <footer className={styles.footer}>{t('landing.footer')}</footer>
    </div>
  );
}
