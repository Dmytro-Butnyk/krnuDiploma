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
    <div className="h-full min-h-0 overflow-auto pr-1 custom-scrollbar">
      <h3 className="text-lg font-black uppercase text-blue-700">2 КРОК: НАЛАШТУВАННЯ ДАТАСУРСІВ</h3>
      <p className="mt-1 max-w-2xl text-sm leading-6 text-slate-500">
        Налаштуйте датасурси: базову сутність, ключ і умови пошуку для отримання даних.
      </p>

      <div className="mt-5 rounded-lg border border-slate-200 bg-slate-50 p-4">
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <label className="block">
            <span className="mb-1 block text-xs font-bold uppercase tracking-wide text-slate-500">Сутність БД</span>
            <select
              value={newSource.entity}
              onChange={(event) => updateNewSource({ entity: event.target.value })}
              className="w-full rounded-md border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
            >
              {entityNames.map((entity) => (
                <option key={entity} value={entity}>
                  {entity}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="mb-1 block text-xs font-bold uppercase tracking-wide text-slate-500">Ключ</span>
            <input
              value={newSource.key}
              onChange={(event) => updateNewSource({ key: event.target.value })}
              className="w-full rounded-md border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500 invalid:border-red-300"
              required
              placeholder="TargetStudent"
            />
          </label>
        </div>

        <div className="mt-4 border-t border-slate-200 pt-4">
          <p className="mb-3 text-xs font-bold uppercase tracking-wide text-slate-500">Умови пошуку</p>
          <div className="space-y-2">
            {newSource.conditions.map((condition, index) => (
              <div key={index} className="grid grid-cols-1 gap-2 md:grid-cols-[1fr_120px_150px_1fr_40px]">
                <select
                  value={condition.property}
                  onChange={(event) => updateCondition(index, { property: event.target.value })}
                  className="rounded-md border border-slate-200 bg-white px-2 py-2 text-sm"
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
                  className="rounded-md border border-slate-200 bg-white px-2 py-2 text-sm"
                >
                  <option value="==">==</option>
                  <option value="!=">!=</option>
                  <option value=".Contains">Contains</option>
                </select>
                <select
                  value={condition.type}
                  onChange={(event) => updateCondition(index, { type: event.target.value as FilterConditionType })}
                  className="rounded-md border border-slate-200 bg-white px-2 py-2 text-sm"
                >
                  <option value="arg">Аргумент API</option>
                  <option value="const">Константа</option>
                </select>
                <input
                  value={condition.value}
                  onChange={(event) => updateCondition(index, { value: event.target.value })}
                  className="rounded-md border border-slate-200 bg-white px-2 py-2 text-sm outline-none focus:border-blue-500 invalid:border-red-300"
                  required
                  placeholder={condition.type === 'arg' ? 'StudentId' : 'Значення'}
                />
                <button
                  onClick={() => removeCondition(index)}
                  className="flex h-10 w-10 items-center justify-center rounded-md text-red-500 hover:bg-red-50"
                  title="Видалити умову"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            ))}
          </div>

          <div className="mt-4 flex flex-wrap justify-between gap-3">
            <Button variant="secondary" className="min-h-8 rounded-full px-5 py-1 text-xs" onClick={addCondition}>
              <Plus size={16} />
              Додати умову
            </Button>
            <div className="flex flex-col items-end gap-2">
              <Button
                className="min-h-8 rounded-full px-5 py-1 text-xs"
                onClick={handleAdd}
                disabled={Boolean(validationReason)}
              >
                Додати сутність
              </Button>
              {validationReason && (
                <span className="max-w-md text-right text-xs font-semibold text-red-500">
                  {validationReason}
                </span>
              )}
            </div>
          </div>
        </div>
      </div>

      <ul className="mt-5 space-y-3">
        {config.DataSources.map((source) => (
          <li key={source.Key} className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
            <div className="flex items-center justify-between gap-3">
              <div>
                <span className="font-black text-blue-700">{source.Key}</span>
                <span className="ml-2 text-xs font-semibold text-slate-500">({source.Entity})</span>
              </div>
              <Button variant="danger" className="min-h-8 px-3 py-1 text-xs" onClick={() => removeDataSource(source.Key)}>
                Видалити
              </Button>
            </div>
            <div className="mt-2 rounded bg-slate-50 p-2 font-mono text-xs text-slate-600">
              Filter: {source.Filter ?? 'null'} | Args: {JSON.stringify(source.FilterArgs)}
            </div>
          </li>
        ))}
      </ul>
    </div>
  )
}
