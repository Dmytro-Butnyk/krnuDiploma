import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import type { EntitySchema } from '../../../entities/schema/model/types'
import type {
  ConstructorStep,
  DataSourceFilterOperator,
  DataSourceConfig,
  EntitySelectInputConfig,
  InputConfig,
  MappingMode,
  NewDataSourceDraft,
  NewInputDraft,
  TagKind,
  TemplateConfiguration,
} from './types'

const initialConfig: TemplateConfiguration = {
  ConfigurationVersion: 2,
  Inputs: {},
  Mapping: { Tables: {}, Scalars: {} },
  DataSources: [],
}

function createDefaultInput(defaultEntity = ''): NewInputDraft {
  return {
    key: '',
    kind: 'Manual',
    entity: defaultEntity,
    valueType: 'String',
    label: '',
    required: true,
    maxLength: '',
    display: [],
    description: [],
    search: [],
    orderBy: [],
    dependsOn: [],
    filters: [],
  }
}

function createDefaultDataSource(defaultEntity = '', inputKey = ''): NewDataSourceDraft {
  return {
    entity: defaultEntity,
    key: defaultEntity ? `Target${defaultEntity}` : '',
    inputKey,
    filterProperty: 'Id',
    filterOperator: 'Equals',
    argumentLabel: defaultEntity,
    parentFilterProperties: [],
  }
}

export function getTableTagPrefix(tag: string) {
  const [prefix, ...rest] = tag.split('.')
  return rest.length > 0 ? prefix : null
}

export function getTableRowTagName(tag: string) {
  const [, ...rest] = tag.split('.')
  return rest.length > 0 ? rest.join('.') : tag
}

function isReservedNumberTag(tag: string) {
  return tag === 'Number' || tag.endsWith('.Number')
}

function normalizeRowMapping(rowMapping: Record<string, string>) {
  return Object.entries(rowMapping).reduce<Record<string, string>>((acc, [tag, path]) => {
    const rowTag = getTableRowTagName(tag)
    if (!(rowTag in acc) || tag === rowTag) {
      acc[rowTag] = path
    }
    return acc
  }, {})
}

function normalizeConfiguration(config: TemplateConfiguration): TemplateConfiguration {
  return {
    ...config,
    ConfigurationVersion: 2,
    Inputs: config.Inputs ?? {},
    Mapping: {
      ...config.Mapping,
      Tables: Object.fromEntries(
        Object.entries(config.Mapping.Tables).map(([tableName, table]) => [
          tableName,
          { ...table, RowMapping: normalizeRowMapping(table.RowMapping) },
        ]),
      ),
    },
    DataSources: config.DataSources.map((source) => ({
      ...source,
      Includes: [...source.Includes],
    })),
  }
}

function getUniqueTableName(baseName: string, tables: Record<string, unknown>) {
  if (!tables[baseName]) return baseName

  let index = 2
  while (tables[`${baseName}_${index}`]) {
    index += 1
  }

  return `${baseName}_${index}`
}

function compactIncludes(includes: string[]) {
  return includes.filter(
    (include) => !includes.some((candidate) => candidate !== include && candidate.startsWith(`${include}.`)),
  )
}

function getUniqueKey(baseKey: string, existingKeys: Iterable<string>) {
  const usedKeys = new Set(existingKeys)
  if (!usedKeys.has(baseKey)) return baseKey

  let index = 2
  while (usedKeys.has(`${baseKey}${index}`)) {
    index += 1
  }

  return `${baseKey}${index}`
}

function isIntField(schema: EntitySchema, entity: string, field: string) {
  const node = schema[entity]
  if (!node) return field === 'Id' || field.endsWith('Id')

  return (
    node.keyScalars.includes(field) ||
    node.foreignKeys.some((foreignKey) => foreignKey.property === field) ||
    field === 'Id' ||
    field.endsWith('Id')
  )
}

function getDefaultOrderBy(schema: EntitySchema, entity: string) {
  return schema[entity]?.displayCandidates?.length ? schema[entity].displayCandidates : ['Id']
}

function getDefaultSearch(schema: EntitySchema, entity: string) {
  const candidates = schema[entity]?.displayCandidates ?? []
  return candidates.length > 0 ? candidates : ['Id']
}

function createEntitySelectInput({
  entity,
  label,
  schema,
  dependsOn = [],
  filters = [],
}: {
  entity: string
  label: string
  schema: EntitySchema
  dependsOn?: string[]
  filters?: EntitySelectInputConfig['Filters']
}): EntitySelectInputConfig {
  return {
    Kind: 'EntitySelect',
    Entity: entity,
    ValueType: 'Int',
    Label: label || entity,
    Required: true,
    DependsOn: dependsOn,
    Filters: filters,
    Display: getDefaultSearch(schema, entity),
    Search: getDefaultSearch(schema, entity),
    OrderBy: getDefaultOrderBy(schema, entity),
  }
}

function createManualInput(label: string): InputConfig {
  return {
    Kind: 'Manual',
    ValueType: 'String',
    Label: label,
    Required: true,
  }
}

function buildFilterExpression(field: string, operator: DataSourceFilterOperator) {
  if (operator === 'NotEquals') return `${field} != @0`
  if (operator === 'Contains') return `${field} != null && ${field}.Contains(@0)`
  return `${field} == @0`
}

function getDataSourceInputKey(entity: string, field: string) {
  return `${entity}${field}`
}

type StoreState = {
  currentStep: ConstructorStep
  mappingMode: MappingMode
  selectedTag: string | null
  selectedTable: string | null
  newTableName: string
  newColumnTag: string
  newColumnPath: string
  searchQuery: string
  expandedSources: Record<string, boolean>
  tagTypes: Record<string, TagKind>
  newInput: NewInputDraft
  newSource: NewDataSourceDraft
  config: TemplateConfiguration
  constructorSessionKey: string | null
  initialize: (payload: {
    tags: string[]
    config?: TemplateConfiguration
    defaultEntity?: string
    sessionKey: string
  }) => void
  hydrateTags: (tags: string[]) => void
  setStep: (step: ConstructorStep) => void
  nextStep: () => void
  previousStep: () => void
  setMappingMode: (mode: MappingMode) => void
  setSelectedTag: (tag: string | null) => void
  setSelectedTable: (tableName: string | null) => void
  setNewTableName: (name: string) => void
  setNewColumnTag: (tag: string) => void
  setNewColumnPath: (path: string) => void
  setSearchQuery: (query: string) => void
  setTagType: (tag: string, type: TagKind) => void
  updateNewInput: (patch: Partial<NewInputDraft>) => void
  setNewInputArrayField: (field: 'display' | 'description' | 'search' | 'orderBy' | 'dependsOn', value: string) => void
  setNewInputFilter: (property: string, inputKey: string) => void
  validateNewInput: () => { ok: true } | { ok: false; reason: string }
  addInput: () => { ok: true } | { ok: false; reason: string }
  removeInput: (key: string) => void
  updateNewSource: (patch: Partial<NewDataSourceDraft>) => void
  toggleParentFilterProperty: (property: string) => void
  validateNewDataSource: () => { ok: true } | { ok: false; reason: string }
  addDataSource: (schema?: EntitySchema) => { ok: true } | { ok: false; reason: string }
  removeDataSource: (key: string) => void
  toggleExpanded: (key: string) => void
  mapScalar: (tag: string, fullPath: string) => void
  mapInputScalar: (tag: string, label: string) => void
  unmapScalar: (tag: string) => void
  createNewTable: () => void
  deleteTable: (tableName: string) => void
  renameTable: (oldName: string, newName: string) => void
  updateTableSourceArray: (tableName: string, sourceArray: string) => void
  addColumnToTable: () => void
  removeColumnFromTable: (tableName: string, tag: string) => void
  calculateIncludes: (schema?: EntitySchema) => void
  reset: () => void
}

function buildInput(draft: NewInputDraft) {
  const key = draft.key.trim()
  const label = draft.label.trim() || key

  if (draft.kind === 'Manual') {
    const maxLength = Number(draft.maxLength)
    return {
      Key: key,
      Config: {
        Kind: 'Manual',
        ValueType: draft.valueType,
        Label: label,
        Required: draft.required,
        ...(Number.isFinite(maxLength) && maxLength > 0 ? { MaxLength: maxLength } : {}),
      },
    } as const
  }

  const config: EntitySelectInputConfig = {
    Kind: 'EntitySelect',
    Entity: draft.entity,
    ValueType: draft.valueType,
    Label: label,
    Required: draft.required,
    DependsOn: draft.dependsOn,
    Filters: draft.filters,
    Display: draft.display,
    Description: draft.description,
    Search: draft.search,
    OrderBy: draft.orderBy,
  }

  return { Key: key, Config: config } as const
}

function buildDataSource(draft: NewDataSourceDraft): DataSourceConfig {
  return {
    Key: draft.key.trim(),
    Entity: draft.entity,
    Filter: buildFilterExpression(draft.filterProperty, draft.filterOperator ?? 'Equals'),
    FilterArgs: [draft.inputKey],
    Includes: [],
  }
}

function validateNewInputDraft(draft: NewInputDraft, config: TemplateConfiguration) {
  const key = draft.key.trim()
  if (!key) return 'Вкажіть ключ інпута.'
  if (key === 'Input') return 'Ключ інпута не може бути "Input".'
  if (config.Inputs[key]) return 'Інпут із таким ключем уже існує.'
  if (!draft.label.trim()) return 'Вкажіть назву поля для користувача.'
  if (!draft.valueType) return 'Оберіть тип значення.'

  if (draft.kind === 'EntitySelect') {
    if (!draft.entity) return 'Оберіть сутність бази даних.'
    if (draft.filters.some((filter) => !filter.Property || !filter.Input)) {
      return 'Заповніть усі фільтри залежного вибору.'
    }
    if (draft.dependsOn.some((dependency) => !config.Inputs[dependency])) {
      return 'Залежність має посилатися на існуючий інпут.'
    }
  }

  return null
}

function validateNewDataSourceDraft(draft: NewDataSourceDraft) {
  if (!draft.entity) return 'Оберіть сутність бази даних.'
  if (!draft.key.trim()) return 'Вкажіть ключ датасурсу.'
  if (draft.key.trim() === 'Input') return 'Ключ датасурсу не може бути "Input".'
  if (!draft.filterProperty.trim()) return 'Оберіть поле фільтра.'
  if (!draft.argumentLabel.trim()) return 'Вкажіть назву аргументу для форми генерації.'

  return null
}

function coerceStep(step: number): ConstructorStep {
  return Math.min(4, Math.max(1, step)) as ConstructorStep
}

export function validateTemplateConfiguration(config: TemplateConfiguration, schema: EntitySchema = {}) {
  const errors: string[] = []
  const inputKeys = Object.keys(config.Inputs)
  const dataSourceKeys = config.DataSources.map((source) => source.Key)
  const dataSourceKeySet = new Set(dataSourceKeys)

  if (config.ConfigurationVersion !== 2) errors.push('Версія конфігурації має бути 2.')

  inputKeys.forEach((key) => {
    if (!key.trim()) errors.push('Ключ інпута не може бути порожнім.')
    if (key === 'Input') errors.push('Ключ інпута не може бути "Input".')

    const input = config.Inputs[key]
    if (!input.Kind || !input.ValueType || !input.Label) {
      errors.push(`Інпут "${key}" заповнений не повністю.`)
    }

    if (input.Kind === 'EntitySelect') {
      if (!schema[input.Entity]) errors.push(`Сутність "${input.Entity}" для інпута "${key}" відсутня у схемі.`)
      input.DependsOn?.forEach((dependency) => {
        if (!config.Inputs[dependency]) errors.push(`Інпут "${key}" залежить від неіснуючого "${dependency}".`)
      })
      input.Filters?.forEach((filter) => {
        if (filter.Operator !== 'Equals') errors.push(`Фільтр інпута "${key}" має використовувати оператор Equals.`)
        if (!config.Inputs[filter.Input]) errors.push(`Фільтр інпута "${key}" посилається на неіснуючий "${filter.Input}".`)
      })
    }
  })

  if (new Set(dataSourceKeys).size !== dataSourceKeys.length) {
    errors.push('Ключі датасурсів мають бути унікальними.')
  }

  config.DataSources.forEach((source) => {
    if (!source.Key.trim()) errors.push('Ключ датасурсу не може бути порожнім.')
    if (source.Key === 'Input') errors.push('Ключ датасурсу не може бути "Input".')
    if (!schema[source.Entity]) errors.push(`Сутність "${source.Entity}" для датасурсу "${source.Key}" відсутня у схемі.`)
    source.FilterArgs.forEach((inputKey) => {
      if (!config.Inputs[inputKey]) errors.push(`Датасурс "${source.Key}" посилається на неіснуючий інпут "${inputKey}".`)
    })
  })

  const validateRoot = (path: string, label: string) => {
    const [root] = path.split('.')
    if (!root) errors.push(`${label}: шлях не може бути порожнім.`)
    if (root === 'Input') {
      const [, inputKey] = path.split('.')
      if (!inputKey || !config.Inputs[inputKey]) errors.push(`${label}: інпут "${inputKey ?? ''}" не існує.`)
    }
    if (root && root !== 'Input' && !dataSourceKeySet.has(root)) {
      errors.push(`${label}: корінь "${root}" не є інпутом або датасурсом.`)
    }
  }

  Object.entries(config.Mapping.Scalars).forEach(([tag, path]) => {
    validateRoot(path, `Скалярний тег "${tag}"`)
  })

  Object.entries(config.Mapping.Tables).forEach(([tableName, table]) => {
    validateRoot(table.SourceArray, `Таблиця "${tableName}"`)
    Object.entries(table.RowMapping).forEach(([tag, path]) => {
      if (!path.trim()) errors.push(`Колонка "${tag}" у таблиці "${tableName}" має порожній шлях.`)
    })
  })

  return Array.from(new Set(errors))
}

export const useConstructorStore = create<StoreState>()(
persist(
  (set, get) => ({
  currentStep: 1,
  mappingMode: 'scalars',
  selectedTag: null,
  selectedTable: null,
  newTableName: '',
  newColumnTag: '',
  newColumnPath: '',
  searchQuery: '',
  expandedSources: {},
  tagTypes: {},
  newInput: createDefaultInput(),
  newSource: createDefaultDataSource(),
  config: initialConfig,
  constructorSessionKey: null,
  initialize: ({ tags, config, defaultEntity, sessionKey }) =>
    set((state) => {
      if (state.constructorSessionKey === sessionKey) {
        const nextTags = { ...state.tagTypes }
        tags.forEach((tag) => {
          nextTags[tag] = isReservedNumberTag(tag) ? 'reserved' : nextTags[tag] ?? 'db_scalar'
        })
        return { tagTypes: nextTags }
      }

      return {
        currentStep: 1,
        mappingMode: 'scalars',
        selectedTag: null,
        selectedTable: null,
        newTableName: '',
        newColumnTag: '',
        newColumnPath: '',
        searchQuery: '',
        expandedSources: Object.fromEntries((config?.DataSources ?? []).map((source) => [source.Key, true])),
        tagTypes: Object.fromEntries(
          tags.map((tag) => [
            tag,
            isReservedNumberTag(tag)
              ? 'reserved'
              : config
                ? (config.Mapping.Scalars[tag]?.startsWith('Input.') ? 'input_scalar' : config.Mapping.Scalars[tag] ? 'db_scalar' : 'table_column')
                : 'db_scalar',
          ]),
        ),
        config: config ? normalizeConfiguration(config) : initialConfig,
        constructorSessionKey: sessionKey,
        newInput: createDefaultInput(defaultEntity ?? ''),
        newSource: createDefaultDataSource(defaultEntity ?? ''),
      }
    }),
  hydrateTags: (tags) =>
    set((state) => {
      const nextTags = { ...state.tagTypes }
      tags.forEach((tag) => {
        nextTags[tag] = isReservedNumberTag(tag) ? 'reserved' : nextTags[tag] ?? 'db_scalar'
      })
      return { tagTypes: nextTags }
    }),
  setStep: (step) => set({ currentStep: step }),
  nextStep: () => {
    const { currentStep } = get()
    set({ currentStep: coerceStep(currentStep + 1) })
  },
  previousStep: () => set((state) => ({ currentStep: coerceStep(state.currentStep - 1) })),
  setMappingMode: (mappingMode) => set({ mappingMode }),
  setSelectedTag: (selectedTag) => set({ selectedTag }),
  setSelectedTable: (selectedTable) =>
    set((state) => ({
      selectedTable,
      newTableName: selectedTable ?? state.newTableName,
    })),
  setNewTableName: (newTableName) => set({ newTableName }),
  setNewColumnTag: (newColumnTag) => set({ newColumnTag }),
  setNewColumnPath: (newColumnPath) => set({ newColumnPath }),
  setSearchQuery: (searchQuery) => set({ searchQuery }),
  setTagType: (tag, type) =>
    set((state) => ({
      tagTypes: { ...state.tagTypes, [tag]: isReservedNumberTag(tag) || state.tagTypes[tag] === 'reserved' ? 'reserved' : type },
    })),
  updateNewInput: (patch) =>
    set((state) => ({
      newInput: { ...state.newInput, ...patch },
    })),
  setNewInputArrayField: (field, value) =>
    set((state) => ({
      newInput: {
        ...state.newInput,
        [field]: value
          .split(',')
          .map((item) => item.trim())
          .filter(Boolean),
      },
    })),
  setNewInputFilter: (property, inputKey) =>
    set((state) => ({
      newInput: {
        ...state.newInput,
        dependsOn: inputKey ? [inputKey] : [],
        filters:
          property && inputKey
            ? [{ Property: property, Operator: 'Equals', Input: inputKey }]
            : [],
      },
    })),
  validateNewInput: () => {
    const reason = validateNewInputDraft(get().newInput, get().config)
    return reason ? { ok: false, reason } : { ok: true }
  },
  addInput: () => {
    const { newInput, config } = get()
    const reason = validateNewInputDraft(newInput, config)
    if (reason) return { ok: false, reason }

    const input = buildInput(newInput)
    set((state) => ({
      config: {
        ...state.config,
        Inputs: { ...state.config.Inputs, [input.Key]: input.Config },
      },
      newInput: createDefaultInput(state.newInput.entity),
      newSource:
        state.newSource.inputKey || state.newSource.entity
          ? state.newSource
          : createDefaultDataSource(state.newInput.entity, input.Key),
    }))

    return { ok: true }
  },
  removeInput: (key) =>
    set((state) => {
      const inputs = { ...state.config.Inputs }
      delete inputs[key]

      const scalars = { ...state.config.Mapping.Scalars }
      Object.entries(scalars).forEach(([tag, path]) => {
        if (path === `Input.${key}`) delete scalars[tag]
      })

      return {
        config: {
          ...state.config,
          Inputs: Object.fromEntries(
            Object.entries(inputs).map(([inputKey, input]) => {
              if (input.Kind !== 'EntitySelect') return [inputKey, input]
              return [
                inputKey,
                {
                  ...input,
                  DependsOn: input.DependsOn?.filter((dependency) => dependency !== key),
                  Filters: input.Filters?.filter((filter) => filter.Input !== key),
                },
              ]
            }),
          ),
          DataSources: state.config.DataSources.filter((source) => !source.FilterArgs.includes(key)),
          Mapping: { ...state.config.Mapping, Scalars: scalars },
        },
      }
    }),
  updateNewSource: (patch) =>
    set((state) => ({
      newSource: { ...state.newSource, ...patch },
    })),
  toggleParentFilterProperty: (property) =>
    set((state) => {
      const parentFilterProperties = state.newSource.parentFilterProperties ?? []
      const selected = parentFilterProperties.includes(property)
      return {
        newSource: {
          ...state.newSource,
          parentFilterProperties: selected
            ? parentFilterProperties.filter((candidate) => candidate !== property)
            : [...parentFilterProperties, property],
        },
      }
    }),
  validateNewDataSource: () => {
    const reason = validateNewDataSourceDraft(get().newSource)
    return reason ? { ok: false, reason } : { ok: true }
  },
  addDataSource: (schema = {}) => {
    const { newSource, config } = get()
    const key = newSource.key.trim()
    const reason = validateNewDataSourceDraft(newSource)
    if (reason) return { ok: false, reason }

    if (!key || !newSource.entity) return { ok: false, reason: 'Вкажіть сутність і ключ датасурсу.' }
    if (key === 'Input') return { ok: false, reason: 'Ключ датасурсу не може бути "Input".' }
    if (config.DataSources.some((source) => source.Key === key)) {
      return { ok: false, reason: 'Датасурс із таким ключем уже існує.' }
    }

    const filterInputKey = getUniqueKey(
      getDataSourceInputKey(newSource.entity, newSource.filterProperty),
      Object.keys(config.Inputs),
    )
    const filterInput =
      isIntField(schema, newSource.entity, newSource.filterProperty)
        ? createEntitySelectInput({
            entity: newSource.entity,
            label: newSource.argumentLabel,
            schema,
          })
        : createManualInput(newSource.argumentLabel)

    const parentInputs = (schema[newSource.entity]?.foreignKeys ?? [])
      .filter((foreignKey) => (newSource.parentFilterProperties ?? []).includes(foreignKey.property))
      .reduce<Record<string, InputConfig>>((acc, foreignKey) => {
        const parentInputKey = getUniqueKey(
          getDataSourceInputKey(foreignKey.targetEntity, 'Id'),
          [...Object.keys(config.Inputs), filterInputKey, ...Object.keys(acc)],
        )
        acc[parentInputKey] = createEntitySelectInput({
          entity: foreignKey.targetEntity,
          label: foreignKey.targetEntity,
          schema,
        })
        return acc
      }, {})

    const parentFilters = (schema[newSource.entity]?.foreignKeys ?? [])
      .filter((foreignKey) => (newSource.parentFilterProperties ?? []).includes(foreignKey.property))
      .map((foreignKey) => {
        const parentInputKey = Object.entries(parentInputs).find(([, input]) => (
          input.Kind === 'EntitySelect' && input.Entity === foreignKey.targetEntity
        ))?.[0]
        return parentInputKey
          ? { Property: foreignKey.property, Operator: 'Equals' as const, Input: parentInputKey }
          : null
      })
      .filter((filter): filter is NonNullable<typeof filter> => Boolean(filter))

    const nextFilterInput: InputConfig =
      filterInput.Kind === 'EntitySelect'
        ? {
            ...filterInput,
            DependsOn: parentFilters.map((filter) => filter.Input),
            Filters: parentFilters,
          }
        : filterInput

    const dataSource = buildDataSource({ ...newSource, inputKey: filterInputKey })
    set((state) => ({
      config: {
        ...state.config,
        Inputs: {
          ...state.config.Inputs,
          ...parentInputs,
          [filterInputKey]: nextFilterInput,
        },
        DataSources: [...state.config.DataSources, dataSource],
      },
      expandedSources: { ...state.expandedSources, [dataSource.Key]: true },
      newSource: createDefaultDataSource(state.newSource.entity),
    }))

    return { ok: true }
  },
  removeDataSource: (key) =>
    set((state) => ({
      config: {
        ...state.config,
        DataSources: state.config.DataSources.filter((source) => source.Key !== key),
      },
    })),
  toggleExpanded: (key) =>
    set((state) => ({
      expandedSources: { ...state.expandedSources, [key]: !state.expandedSources[key] },
    })),
  mapScalar: (tag, fullPath) =>
    set((state) => ({
      config: {
        ...state.config,
        Mapping: {
          ...state.config.Mapping,
          Scalars: { ...state.config.Mapping.Scalars, [tag]: fullPath },
        },
      },
    })),
  mapInputScalar: (tag, label) =>
    set((state) => {
      const existingPath = state.config.Mapping.Scalars[tag]
      const existingKey = existingPath?.startsWith('Input.') ? existingPath.slice('Input.'.length) : null
      const inputKey = existingKey ?? getUniqueKey(tag, Object.keys(state.config.Inputs))

      return {
        config: {
          ...state.config,
          Inputs: {
            ...state.config.Inputs,
            [inputKey]: createManualInput(label.trim() || tag),
          },
          Mapping: {
            ...state.config.Mapping,
            Scalars: { ...state.config.Mapping.Scalars, [tag]: `Input.${inputKey}` },
          },
        },
      }
    }),
  unmapScalar: (tag) =>
    set((state) => {
      const removedPath = state.config.Mapping.Scalars[tag]
      const removedInputKey = removedPath?.startsWith('Input.') ? removedPath.slice('Input.'.length) : null
      const restScalars = { ...state.config.Mapping.Scalars }
      delete restScalars[tag]
      const inputs = { ...state.config.Inputs }
      if (
        removedInputKey &&
        !Object.entries(restScalars).some(([, path]) => path === `Input.${removedInputKey}`) &&
        !state.config.DataSources.some((source) => source.FilterArgs.includes(removedInputKey))
      ) {
        delete inputs[removedInputKey]
      }
      return {
        selectedTag: state.selectedTag === tag ? null : state.selectedTag,
        config: {
          ...state.config,
          Inputs: inputs,
          Mapping: { ...state.config.Mapping, Scalars: restScalars },
        },
      }
    }),
  createNewTable: () => {
    const { config, tagTypes } = get()
    const usedRowTags = new Set(
      Object.values(config.Mapping.Tables).flatMap((table) => Object.keys(table.RowMapping)),
    )
    const nextPrefixedTag = Object.entries(tagTypes).find(
      ([tag, type]) =>
        type === 'table_column' &&
        Boolean(getTableTagPrefix(tag)) &&
        !usedRowTags.has(getTableRowTagName(tag)),
    )?.[0]
    const tableName = getUniqueTableName(
      getTableTagPrefix(nextPrefixedTag ?? '') ?? `Table_${Math.floor(Math.random() * 1000)}`,
      config.Mapping.Tables,
    )

    set((state) => ({
      selectedTable: tableName,
      newTableName: tableName,
      config: {
        ...state.config,
        Mapping: {
          ...state.config.Mapping,
          Tables: {
            ...state.config.Mapping.Tables,
            [tableName]: { SourceArray: '', RowMapping: {} },
          },
        },
      },
    }))
  },
  deleteTable: (tableName) =>
    set((state) => {
      const tables = { ...state.config.Mapping.Tables }
      delete tables[tableName]
      return {
        selectedTable: state.selectedTable === tableName ? null : state.selectedTable,
        config: {
          ...state.config,
          Mapping: { ...state.config.Mapping, Tables: tables },
        },
      }
    }),
  renameTable: (oldName, newName) =>
    set((state) => {
      const trimmedName = newName.trim()
      if (!trimmedName || oldName === trimmedName || state.config.Mapping.Tables[trimmedName]) return state

      const { [oldName]: table, ...tables } = state.config.Mapping.Tables
      if (!table) return state

      return {
        selectedTable: trimmedName,
        newTableName: trimmedName,
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: { ...tables, [trimmedName]: table },
          },
        },
      }
    }),
  updateTableSourceArray: (tableName, sourceArray) =>
    set((state) => {
      const table = state.config.Mapping.Tables[tableName]
      if (!table) return state

      return {
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: {
              ...state.config.Mapping.Tables,
              [tableName]: { ...table, SourceArray: sourceArray },
            },
          },
        },
      }
    }),
  addColumnToTable: () =>
    set((state) => {
      const { selectedTable, newColumnTag, newColumnPath } = state
      if (!selectedTable || !newColumnTag || !newColumnPath) return state

      const table = state.config.Mapping.Tables[selectedTable]
      if (!table) return state

      return {
        newColumnTag: '',
        newColumnPath: '',
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: {
              ...state.config.Mapping.Tables,
              [selectedTable]: {
                ...table,
                RowMapping: {
                  ...table.RowMapping,
                  [getTableRowTagName(newColumnTag)]: newColumnPath,
                },
              },
            },
          },
        },
      }
    }),
  removeColumnFromTable: (tableName, tag) =>
    set((state) => {
      const table = state.config.Mapping.Tables[tableName]
      if (!table) return state

      const rowMapping = { ...table.RowMapping }
      delete rowMapping[tag]
      return {
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: {
              ...state.config.Mapping.Tables,
              [tableName]: { ...table, RowMapping: rowMapping },
            },
          },
        },
      }
    }),
  calculateIncludes: (schema = {}) =>
    set((state) => {
      const tables = Object.fromEntries(
        Object.entries(state.config.Mapping.Tables).map(([tableName, table]) => [
          tableName,
          { ...table, RowMapping: normalizeRowMapping(table.RowMapping) },
        ]),
      )
      const dataSources: DataSourceConfig[] = state.config.DataSources.map((source) => ({
        ...source,
        Includes: [],
      }))

      const addInclude = (dataSourceKey: string, include: string) => {
        const dataSource = dataSources.find((source) => source.Key === dataSourceKey)
        if (dataSource && !dataSource.Includes.includes(include)) {
          dataSource.Includes.push(include)
        }
      }

      const processPath = (fullPath: string) => {
        const parts = fullPath.split('.')
        if (parts.length < 3) return

        const [dataSourceKey, ...pathParts] = parts
        const dataSource = dataSources.find((source) => source.Key === dataSourceKey)
        if (!dataSource) return

        let currentEntity = dataSource.Entity
        const includeParts: string[] = []

        pathParts.slice(0, -1).forEach((part) => {
          const node = schema[currentEntity]
          if (!node) {
            includeParts.push(part)
            return
          }

          const nextEntity = node.entities[part] ?? node.collections[part]
          if (!nextEntity) return

          includeParts.push(part)
          currentEntity = nextEntity
        })

        if (includeParts.length > 0) {
          addInclude(dataSourceKey, includeParts.join('.'))
        }
      }

      Object.values(state.config.Mapping.Scalars).forEach(processPath)

      Object.values(tables).forEach((table) => {
        if (!table.SourceArray) return

        processPath(`${table.SourceArray}.FakeProp`)
        Object.values(table.RowMapping).forEach((columnPath) => {
          processPath(`${table.SourceArray}.${columnPath}`)
        })
      })

      return {
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: tables,
          },
          DataSources: dataSources.map((source) => ({
            ...source,
            Includes: compactIncludes(source.Includes),
          })),
        },
      }
    }),
  reset: () =>
    set({
      currentStep: 1,
      mappingMode: 'scalars',
      selectedTag: null,
      selectedTable: null,
      newTableName: '',
      newColumnTag: '',
      newColumnPath: '',
      searchQuery: '',
      expandedSources: {},
      tagTypes: {},
      config: initialConfig,
      constructorSessionKey: null,
      newInput: createDefaultInput(),
      newSource: createDefaultDataSource(),
    }),
  }),
  {
    name: 'template-constructor-state',
    storage: createJSONStorage(() => sessionStorage),
    partialize: (state) => ({
      currentStep: state.currentStep,
      mappingMode: state.mappingMode,
      selectedTag: state.selectedTag,
      selectedTable: state.selectedTable,
      newTableName: state.newTableName,
      newColumnTag: state.newColumnTag,
      newColumnPath: state.newColumnPath,
      searchQuery: state.searchQuery,
      expandedSources: state.expandedSources,
      tagTypes: state.tagTypes,
      newInput: state.newInput,
      newSource: state.newSource,
      config: state.config,
      constructorSessionKey: state.constructorSessionKey,
    }),
  },
))
