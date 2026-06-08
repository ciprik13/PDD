import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TreatmentPanel } from '@/components/treatment/TreatmentPanel';
import { useTreatments } from '@/hooks/useApi';
import type { DiseaseClass } from '@/types';
import styles from './shared.module.css';

const DISEASES: DiseaseClass[] = [
  'late_blight',
  'early_blight',
  'fusarium_wilt',
  'powdery_mildew',
  'bacterial_spot',
  'leaf_mold',
  'septoria_leaf_spot',
  'spider_mites',
];

export function TreatmentsPage() {
  const { t } = useTranslation();
  const [disease, setDisease] = useState<DiseaseClass>('late_blight');
  const { data: treatments, loading } = useTreatments(disease);

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('recommendations.title')}</h1>
          <p className={styles.pageSub}>{t('recommendations.subtitle')}</p>
        </div>
        <select
          className={styles.select}
          value={disease}
          onChange={(e) => setDisease(e.target.value as DiseaseClass)}
        >
          {DISEASES.map((d) => (
            <option key={d} value={d}>{t(`disease.${d}`)}</option>
          ))}
        </select>
      </div>

      {loading && <div className={styles.card}><p className={styles.empty}>{t('common.loading')}</p></div>}
      {!loading && (treatments?.length ?? 0) === 0 && (
        <div className={styles.card}><p className={styles.empty}>{t('recommendations.empty')}</p></div>
      )}
      {!loading && treatments && treatments.length > 0 && (
        <TreatmentPanel treatments={treatments} diseaseName={t(`disease.${disease}`)} />
      )}
    </div>
  );
}
