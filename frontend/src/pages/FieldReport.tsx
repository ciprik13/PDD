import { useTranslation } from 'react-i18next';
import { FileDown, Map } from 'lucide-react';
import { DetectionList, PassportTimeline } from '@/components/detection/DetectionList';
import { TreatmentPanel } from '@/components/treatment/TreatmentPanel';
import { useDashboardStats, useDetections, useTreatments, usePassport } from '@/hooks/useApi';
import styles from './FieldReport.module.css';
import shared from './shared.module.css';

export function FieldReportPage() {
  const { t, i18n } = useTranslation();
  const { data: stats } = useDashboardStats();
  const { data: detections } = useDetections(20);

  const criticalCount = detections?.filter(d => d.severity === 'critical').length ?? 0;
  const warningCount = detections?.filter(d => d.severity === 'warning').length ?? 0;

  // Focus the report on the most recent diseased plant (falls back to the latest detection).
  const focus = detections?.find(d => d.severity !== 'healthy') ?? detections?.[0] ?? null;
  const { data: treatments } = useTreatments(
    focus && focus.severity !== 'healthy' ? focus.topPrediction.diseaseClass : null);
  const { data: passport } = usePassport(focus?.plantId ?? null);

  return (
    <div className={styles.page}>
      {/* Top bar */}
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('fieldReport.title')}</h1>
          <p className={styles.pageSub}>
            {t('fieldReport.subtitle')} ·{' '}
            {new Date().toLocaleDateString(i18n.language, { day: 'numeric', month: 'long', year: 'numeric' })}
          </p>
        </div>
        <div className={styles.tbRight}>
          <button
            className={styles.exportBtn}
            onClick={() => {
              const prev = document.title;
              document.title = `AgriCure — ${t('fieldReport.title')} · ${new Date().toLocaleDateString()}`;
              window.print();
              document.title = prev;
            }}
          >
            <FileDown size={14} />
            {t('fieldReport.export')}
          </button>
        </div>
      </div>

      {/* Summary chips */}
      <div className={styles.summaryRow}>
        <span className={[styles.summaryChip, styles['summaryChip-b']].join(' ')}>
          {t('fieldReport.summary.total', { count: stats?.detectionsToday ?? 0 })}
        </span>
        {criticalCount > 0 && (
          <span className={[styles.summaryChip, styles['summaryChip-r']].join(' ')}>
            {t('fieldReport.summary.critical', { count: criticalCount })}
          </span>
        )}
        {warningCount > 0 && (
          <span className={[styles.summaryChip, styles['summaryChip-a']].join(' ')}>
            {t('fieldReport.summary.warning', { count: warningCount })}
          </span>
        )}
        <span className={[styles.summaryChip, styles['summaryChip-g']].join(' ')}>
          {t('fieldReport.summary.rows', { scanned: stats?.rowsScanned ?? 0, total: stats?.totalRows ?? 0 })}
        </span>
      </div>

      {/* Main grid: detection log + treatment */}
      <div className={styles.mainGrid}>
        <DetectionList />
        {treatments && treatments.length > 0 && focus
          ? <TreatmentPanel treatments={treatments} diseaseName={t(`disease.${focus.topPrediction.diseaseClass}`)} />
          : <div className={shared.card}><p className={shared.empty}>{t('recommendations.empty')}</p></div>}
      </div>

      {/* Bottom grid: passport + zone map */}
      <div className={styles.bottomGrid}>
        {passport
          ? <PassportTimeline passport={passport} />
          : <div className={shared.card}><p className={shared.empty}>{t('plants.empty')}</p></div>}
        <div className={styles.futureMap}>
          <div className={styles.futureMapIcon}>
            <Map size={22} />
          </div>
          <p className={styles.futureLabel}>{t('dashboard.zoneMap')}</p>
          <p className={styles.futureSub}>{t('dashboard.zoneMapDesc')}</p>
        </div>
      </div>
    </div>
  );
}
