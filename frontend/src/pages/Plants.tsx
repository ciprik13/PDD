import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { formatDistanceToNow } from 'date-fns';
import { ro, ru, enUS } from 'date-fns/locale';
import { usePlants } from '@/hooks/useApi';
import type { DetectionSeverity, PlantSummary } from '@/types';
import styles from './shared.module.css';

const SEVERITY_CHIP: Record<DetectionSeverity, string> = {
  critical: 'chip-r',
  warning: 'chip-a',
  healthy: 'chip-g',
};

export function PlantsPage() {
  const { t, i18n } = useTranslation();
  const { data: plants, loading } = usePlants();
  const locale = i18n.language === 'ro' ? ro : i18n.language === 'ru' ? ru : enUS;

  function severityChip(p: PlantSummary) {
    if (!p.latestSeverity) {
      return <span className={[styles.chip, styles['chip-g']].join(' ')}>{t('plants.noScans')}</span>;
    }
    return (
      <span className={[styles.chip, styles[SEVERITY_CHIP[p.latestSeverity]]].join(' ')}>
        {t(`detection.severity.${p.latestSeverity}`)}
      </span>
    );
  }

  function label(p: PlantSummary) {
    if (!p.latestLabel || p.latestSeverity === 'healthy') return t('detection.severity.healthy');
    return p.latestLabel;
  }

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('plants.title')}</h1>
          <p className={styles.pageSub}>{t('plants.subtitle')}</p>
        </div>
      </div>

      <div className={styles.card}>
        {loading && <p className={styles.empty}>{t('common.loading')}</p>}
        {!loading && (plants?.length ?? 0) === 0 && <p className={styles.empty}>{t('plants.empty')}</p>}
        {(plants ?? []).map((p) => (
          <Link key={p.plantId} to={`/passport?plant=${encodeURIComponent(p.plantId)}`} className={styles.listItem} style={{ textDecoration: 'none' }}>
            <div className={styles.listDot} style={{
              background: p.latestSeverity === 'critical' ? '#ef4444'
                : p.latestSeverity === 'warning' ? '#f59e0b'
                : 'var(--forest-3)',
            }} />
            <div style={{ flex: 1 }}>
              <div className={styles.listTitle}>
                {p.plantId}{p.row != null ? ` · ${t('plants.rowShort', { row: p.row })}` : ''} — {label(p)}
              </div>
              <div className={styles.listDesc}>
                {t('plants.detectionCount', { count: p.detectionCount })}
                {p.lastSeenAt ? ` · ${formatDistanceToNow(new Date(p.lastSeenAt), { addSuffix: true, locale })}` : ''}
              </div>
            </div>
            {severityChip(p)}
          </Link>
        ))}
      </div>
    </div>
  );
}
