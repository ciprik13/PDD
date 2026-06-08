import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { PassportTimeline } from '@/components/detection/DetectionList';
import { usePlants, usePassport } from '@/hooks/useApi';
import styles from './shared.module.css';

export function PlantPassportPage() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const { data: plants } = usePlants();
  const [selected, setSelected] = useState<string | null>(searchParams.get('plant'));

  // Default to the first plant once the list loads and nothing is chosen.
  useEffect(() => {
    if (!selected && plants && plants.length > 0) {
      setSelected(plants[0].plantId);
    }
  }, [plants, selected]);

  const { data: passport, loading, error } = usePassport(selected);

  function choose(plantId: string) {
    setSelected(plantId);
    setSearchParams(plantId ? { plant: plantId } : {});
  }

  return (
    <div className={styles.page}>
      <div className={styles.topbar}>
        <div>
          <h1 className={styles.pageHead}>{t('plantPassport.title')}</h1>
          <p className={styles.pageSub}>{t('plantPassport.subtitle')}</p>
        </div>
        {plants && plants.length > 0 && (
          <select
            className={styles.select}
            value={selected ?? ''}
            onChange={(e) => choose(e.target.value)}
          >
            {plants.map((p) => (
              <option key={p.plantId} value={p.plantId}>
                {p.plantId}{p.row != null ? ` · ${t('plants.rowShort', { row: p.row })}` : ''}
              </option>
            ))}
          </select>
        )}
      </div>

      {plants && plants.length === 0 && (
        <div className={styles.card}><p className={styles.empty}>{t('plants.empty')}</p></div>
      )}
      {selected && loading && (
        <div className={styles.card}><p className={styles.empty}>{t('common.loading')}</p></div>
      )}
      {selected && error && !passport && (
        <div className={styles.card}><p className={styles.empty}>{t('common.loadError')}</p></div>
      )}
      {passport && <PassportTimeline passport={passport} />}
    </div>
  );
}
