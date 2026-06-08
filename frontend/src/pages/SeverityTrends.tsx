import { useTranslation } from 'react-i18next';
import { useDetections } from '@/hooks/useApi';
import type { Detection } from '@/types';
import styles from './shared.module.css';
import tStyles from './SeverityTrends.module.css';

function intensity(d: Detection): number {
  if (d.severity === 'healthy') return 0;
  const area = d.boundingBox.affectedAreaPercent;
  return area > 0 ? area : d.topPrediction.confidence * 100;
}

function buildHistory(detections: Detection[]): { date: string; value: number }[] {
  const byDay = new Map<string, number>();
  for (const d of detections) {
    const day = d.timestamp.slice(0, 10);
    byDay.set(day, Math.max(byDay.get(day) ?? 0, intensity(d)));
  }
  return [...byDay.entries()]
    .map(([date, value]) => ({ date, value: Math.round(value) }))
    .sort((a, b) => a.date.localeCompare(b.date))
    .slice(-9);
}

export function SeverityTrendsPage() {
  const { t } = useTranslation();
  const { data: detections, loading } = useDetections(200);
  const list = detections ?? [];

  const critical = list.filter((d) => d.severity === 'critical').length;
  const warning = list.filter((d) => d.severity === 'warning').length;
  const healthy = list.filter((d) => d.severity === 'healthy').length;
  const history = buildHistory(list);

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('severityTrends.title')}</h1>
          <p className={styles.pageSub}>{t('severityTrends.subtitle')}</p>
        </div>
      </div>

      {loading && <div className={styles.card}><p className={styles.empty}>{t('common.loading')}</p></div>}
      {!loading && list.length === 0 && (
        <div className={styles.card}><p className={styles.empty}>{t('severityTrends.empty')}</p></div>
      )}

      {!loading && list.length > 0 && (
        <>
          <div className={tStyles.chipRow}>
            <span className={[styles.chip, styles['chip-r']].join(' ')}>
              {t('severityTrends.critical', { count: critical })}
            </span>
            <span className={[styles.chip, styles['chip-a']].join(' ')}>
              {t('severityTrends.warning', { count: warning })}
            </span>
            <span className={[styles.chip, styles['chip-g']].join(' ')}>
              {t('severityTrends.healthy', { count: healthy })}
            </span>
          </div>

          <div className={styles.card} style={{ marginBottom: 14 }}>
            <div className={styles.cardTitle}>{t('severityTrends.chartTitle')}</div>
            <div className={styles.cardSub}>{t('severityTrends.chartSub')}</div>
            <div className={tStyles.chart}>
              {history.map((pt, i) => (
                <div key={i} className={tStyles.bar}>
                  <div
                    className={tStyles.barFill}
                    style={{
                      height: `${pt.value}%`,
                      background:
                        pt.value > 70 ? '#dc2626'
                        : pt.value > 40 ? '#ef4444'
                        : pt.value > 20 ? '#fca5a5'
                        : 'var(--forest-3)',
                    }}
                  />
                  <span className={tStyles.barLabel}>{pt.date.slice(5)}</span>
                </div>
              ))}
            </div>
          </div>

          <div className={styles.grid3}>
            <div className={styles.card}>
              <div className={styles.statLabel}>{t('severityTrends.criticalLabel')}</div>
              <div className={styles.statValue} style={{ color: '#dc2626' }}>{critical}</div>
              <div className={styles.statUnit}>{t('severityTrends.detections')}</div>
            </div>
            <div className={styles.card}>
              <div className={styles.statLabel}>{t('severityTrends.warningLabel')}</div>
              <div className={styles.statValue} style={{ color: '#d97706' }}>{warning}</div>
              <div className={styles.statUnit}>{t('severityTrends.detections')}</div>
            </div>
            <div className={styles.card}>
              <div className={styles.statLabel}>{t('severityTrends.healthyLabel')}</div>
              <div className={styles.statValue} style={{ color: 'var(--forest-2)' }}>{healthy}</div>
              <div className={styles.statUnit}>{t('severityTrends.detections')}</div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
