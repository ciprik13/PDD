import { useTranslation } from 'react-i18next';
import { formatDistanceToNow } from 'date-fns';
import { ro, ru, enUS } from 'date-fns/locale';
import { useDetections } from '@/hooks/useApi';
import type { Detection } from '@/types';
import styles from './shared.module.css';

export function AlertsPage() {
  const { t, i18n } = useTranslation();
  const { data: detections, loading } = useDetections(50);
  const locale = i18n.language === 'ro' ? ro : i18n.language === 'ru' ? ru : enUS;

  // Alerts are the non-healthy detections, newest first.
  const alerts = (detections ?? []).filter((d) => d.severity !== 'healthy');

  function dot(d: Detection) {
    return d.severity === 'critical' ? '#ef4444' : '#f59e0b';
  }

  function title(d: Detection) {
    return `${d.topPrediction.label} — ${t('alerts.location', { row: d.row, plant: d.plantId })}`;
  }

  function desc(d: Detection) {
    return t('alerts.desc.detected', {
      confidence: (d.topPrediction.confidence * 100).toFixed(1),
      leafArea: Math.round(d.boundingBox.affectedAreaPercent),
      action: t(d.severity === 'critical' ? 'alerts.action.actionRequired' : 'alerts.action.monitorClosely'),
    });
  }

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('alertsPage.title')}</h1>
          <p className={styles.pageSub}>{t('alertsPage.subtitle')}</p>
        </div>
      </div>

      <div className={styles.card}>
        {loading && <p className={styles.empty}>{t('common.loading')}</p>}
        {!loading && alerts.length === 0 && <p className={styles.empty}>{t('alertsPage.empty')}</p>}
        {alerts.map((d) => (
          <div key={d.id} className={styles.listItem}>
            <div className={styles.listDot} style={{ background: dot(d) }} />
            <div>
              <div className={styles.listTitle}>{title(d)}</div>
              <div className={styles.listDesc}>{desc(d)}</div>
              <div className={styles.listTime}>
                {formatDistanceToNow(new Date(d.timestamp), { addSuffix: true, locale })}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
