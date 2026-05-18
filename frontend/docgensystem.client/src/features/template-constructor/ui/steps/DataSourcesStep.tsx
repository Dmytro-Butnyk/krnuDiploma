import { Plus, Trash2 } from 'lucide-react'
import { useEffect, useMemo } from 'react'
import type { EntitySchema } from '../../../../entities/schema/model/types'
import { Button } from '../../../../shared/ui/Button'
import { useConstructorStore } from '../../model/store'
import type { FilterConditionType, FilterOperator } from '../../model/types'

type Props = {
  schema?: EntitySchema
}

export function DataSourcesStep({ schema = {} }: Props) {
  const newSource = useConstructorStore((state) => state.newSource)
  const config = useConstructorStore((state) => state.config)
  const updateNewSource = useConstructorStore((state) => state.updateNewSource)
  const updateCondition = useConstructorStore((state) => state.updateNewSourceCondition)
  const addCondition = useConstructorStore((state) => state.addNewSourceCondition)
  const removeCondition = useConstructorStore((state) => state.removeNewSourceCondition)
  const validateNewDataSource = useConstructorStore((state) => state.validateNewDataSource)
  const addDataSource = useConstructorStore((state) => state.addDataSource)
  const removeDataSource = useConstructorStore((state) => state.removeDataSource)
  const entityNames = useMemo(() => Object.keys(schema), [schema])
  const scalarFields = schema[newSource.entity]?.scalars ?? []
  const validationResult = validateNewDataSource()
  const validationReason = validationResult.ok ? null : validationResult.reason

  useEffect(() => {
    if (!newSource.entity && entityNames[0]) {
      updateNewSource({ entity: entityNames[0] })
    }
  }, [entityNames, newSource.entity, updateNewSource])

  const handleAdd = () => {
    const validation = validateNewDataSource()
    if (!validation.ok) {
      window.alert(validation.reason)
      return
    }

    const result = addDataSource()
    if (!result.ok) window.alert(result.reason)
  }

  return (
    <div className="h-full min-h-0 overflow-y-auto overflow-x-hidden pr-1 custom-scrollbar">
      <h3 className="ui-step-title">2 КРОК: НАЛАШТУВАННЯ ДАТАСУРСІВ</h3>
      <p className="ui-lead mt-8 max-w-3xl">Налаштуйте датасурси</p>

      <div className="mt-8 rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-surface)] p-5 shadow-[var(--shadow-ui)]">
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <label className="block">
            <span className="ui-label mb-2 block">Сутність бази даних</span>
            <select
              value={newSource.entity}
              onChange={(event) => updateNewSource({ entity: event.target.value })}
              className="ui-input w-full px-4 py-3 text-sm font-bold"
            >
              {entityNames.map((entity) => (
                <option key={entity} value={entity}>
                  {entity}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="ui-label mb-2 block">Ключ</span>
            <input
              value={newSource.key}
              onChange={(event) => updateNewSource({ key: event.target.value })}
              className="ui-input w-full px-4 py-3 text-sm font-bold invalid:border-[var(--color-danger)]"
              required
              placeholder="TargetStudent"
            />
          </label>
        </div>

        <div className="mt-5 border-t border-[var(--color-bg-lavender)] pt-5">
          <p className="ui-label mb-3">Умови пошуку</p>
          <div className="space-y-2">
            {newSource.conditions.map((condition, index) => (
              <div key={index} className="grid min-w-0 grid-cols-1 items-center gap-2 md:grid-cols-[minmax(110px,1fr)_minmax(82px,120px)_minmax(132px,150px)_minmax(130px,1fr)_40px]">
                <select
                  value={condition.property}
                  onChange={(event) => updateCondition(index, { property: event.target.value })}
                  className="ui-input min-w-0 px-3 py-2 text-sm"
                >
                  {scalarFields.map((scalar) => (
                    <option key={scalar} value={scalar}>
                      {scalar}
                    </option>
                  ))}
                </select>
                <select
                  value={condition.operator}
                  onChange={(event) => updateCondition(index, { operator: event.target.value as FilterOperator })}
                  className="ui-input min-w-0 px-3 py-2 text-sm"
                >
                  <option value="==">==</option>
                  <option value="!=">!=</option>
                  <option value=".Contains">Contains</option>
                </select>
                <select
                  value={condition.type}
                  onChange={(event) => updateCondition(index, { type: event.target.value as FilterConditionType })}
                  className="ui-input min-w-0 px-3 py-2 text-sm"
                >
                  <option value="arg">Аргумент API</option>
                  <option value="const">Константа</option>
                </select>
                <input
                  value={condition.value}
                  onChange={(event) => updateCondition(index, { value: event.target.value })}
                  className="ui-input min-w-0 px-3 py-2 text-sm invalid:border-[var(--color-danger)]"
                  required
                  placeholder={condition.type === 'arg' ? 'StudentId' : 'Значення'}
                />
                <button
                  onClick={() => removeCondition(index)}
                  className="flex h-10 w-10 shrink-0 items-center justify-center rounded-[12px] text-[var(--color-danger)] transition hover:bg-[var(--color-danger-soft)]"
                  title="Видалити умову"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            ))}
          </div>

          <div className="mt-5 flex flex-wrap items-start justify-between gap-3">
            <Button variant="secondary" size="pill" className="h-[58px] min-h-[58px] text-base" onClick={addCondition}>
              <Plus size={16} />
              Додати умову
            </Button>
            <div className="flex flex-col items-end gap-2">
              <Button variant="primary" size="pill" className="h-[58px] min-h-[58px] text-base" onClick={handleAdd} disabled={Boolean(validationReason)}>
                Додати сутність
              </Button>
              {validationReason && (
                <span className="max-w-md text-right text-xs font-bold text-[var(--color-danger)]">
                  {validationReason}
                </span>
              )}
            </div>
          </div>
        </div>
      </div>

      <ul className="mt-5 space-y-3">
        {config.DataSources.map((source) => (
          <li key={source.Key} className="rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-4 shadow-[var(--shadow-ui)]">
            <div className="flex items-center justify-between gap-3">
              <div>
                <span className="font-extrabold text-[var(--color-primary)]">{source.Key}</span>
                <span className="ml-2 text-xs font-bold text-[var(--color-muted)]">({source.Entity})</span>
              </div>
              <Button variant="danger" size="sm" onClick={() => removeDataSource(source.Key)}>
                Видалити
              </Button>
            </div>
            <div className="mt-3 overflow-x-auto rounded-[12px] bg-[var(--color-bg-lavender)] p-3 font-mono text-xs text-[var(--color-muted)]">
              Filter: {source.Filter ?? 'null'} | Args: {JSON.stringify(source.FilterArgs)}
            </div>
          </li>
        ))}
      </ul>
    </div>
  )
}
