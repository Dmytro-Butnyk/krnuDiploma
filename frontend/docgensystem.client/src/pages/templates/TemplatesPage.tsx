import {
  ArrowLeft,
  CheckCircle2,
  FileText,
  Loader2,
  MoreVertical,
  Plus,
  UploadCloud,
  X,
} from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { useEffect, useMemo, useRef, useState, type Dispatch, type DragEvent, type SetStateAction } from 'react'
import {
  fetchGenerationInputOptions,
  useDeleteTemplate,
  useGenerateDocument,
  useGenerationForm,
  useScanTemplateForTags,
  useTemplateDetails,
  useTemplates,
  useUpdateTemplate,
  useUploadTemplate,
} from '../../entities/template/api/templateApi'
import type { GenerationInputDto, TemplateListItemDto } from '../../entities/template/model/types'
import { useConstructorStore } from '../../features/template-constructor/model/store'
import { TemplateConstructor } from '../../features/template-constructor/ui/TemplateConstructor'
import type { TemplateConfiguration } from '../../features/template-constructor/model/types'
import { getApiErrorMessage } from '../../shared/api/errors'
import { cn } from '../../shared/lib/cn'
import { Button } from '../../shared/ui/Button'

type ScreenMode = 'list' | 'upload' | 'details' | 'constructor'
type ConstructorMode = 'create' | 'edit'
type DialogState = 'generating' | 'template-created' | 'template-updated' | 'document-generated' | 'delete' | null

type DraftTemplate = {
  file: File
  name: string
  tags: string[]
}

type DraftTemplateMeta = {
  fileName: string
  name: string
  size: number
  lastModified: number
  tags: string[]
}

type PersistedTemplatesPageState = {
  mode: ScreenMode
  selectedTemplate: TemplateListItemDto | null
  constructorMode: ConstructorMode
  constructorTemplateName: string
  generationParams: Record<string, string>
  isGenerationFormOpen: boolean
  draftTemplateMeta: DraftTemplateMeta | null
}

const templatesPageStateStorageKey = 'templates-page-state'

const defaultTemplatesPageState: PersistedTemplatesPageState = {
  mode: 'list',
  selectedTemplate: null,
  constructorMode: 'create',
  constructorTemplateName: '',
  generationParams: {},
  isGenerationFormOpen: false,
  draftTemplateMeta: null,
}

function readPersistedTemplatesPageState(): PersistedTemplatesPageState {
  const fallback = defaultTemplatesPageState

  try {
    const raw = sessionStorage.getItem(templatesPageStateStorageKey)
    if (!raw) return fallback

    const parsed = JSON.parse(raw) as Partial<PersistedTemplatesPageState>

    return {
      ...fallback,
      ...parsed,
      mode: parsed.mode ?? fallback.mode,
      selectedTemplate: parsed.selectedTemplate ?? null,
      generationParams: parsed.generationParams ?? {},
      draftTemplateMeta: parsed.draftTemplateMeta ?? null,
    }
  } catch {
    return fallback
  }
}

function sameTemplateId(left: TemplateListItemDto['id'], right: TemplateListItemDto['id']) {
  return String(left) === String(right)
}

type ParsedConfigurationState = {
  configuration?: TemplateConfiguration
  isSupported: boolean
  hasConfiguration: boolean
}

function parseConfiguration(value?: string | null): ParsedConfigurationState {
  if (!value) return { isSupported: false, hasConfiguration: false }

  try {
    const parsed = JSON.parse(value) as Partial<TemplateConfiguration>
    if (
      parsed?.ConfigurationVersion === 2 &&
      parsed.Inputs &&
      parsed.Mapping?.Scalars &&
      parsed.Mapping?.Tables &&
      Array.isArray(parsed.DataSources)
    ) {
      return {
        configuration: parsed as TemplateConfiguration,
        isSupported: true,
        hasConfiguration: true,
      }
    }
  } catch {
    return { isSupported: false, hasConfiguration: true }
  }

  return { isSupported: false, hasConfiguration: true }
}

function getTagsFromConfiguration(configuration?: TemplateConfiguration) {
  if (!configuration) return []

  const scalarTags = Object.keys(configuration.Mapping.Scalars)
  const tableTags = Object.values(configuration.Mapping.Tables).flatMap((table) => Object.keys(table.RowMapping))

  return Array.from(new Set([...scalarTags, ...tableTags]))
}

function getDraftTemplateSessionKey(draftTemplate: DraftTemplate | null) {
  if (!draftTemplate) return 'create:no-file'

  const { file } = draftTemplate
  return `create:${file.name}:${file.size}:${file.lastModified}`
}

function getDraftTemplateMetaSessionKey(draftTemplateMeta: DraftTemplateMeta | null) {
  if (!draftTemplateMeta) return 'create:no-file'

  return `create:${draftTemplateMeta.fileName}:${draftTemplateMeta.size}:${draftTemplateMeta.lastModified}`
}

function createDraftTemplateMeta(draftTemplate: DraftTemplate): DraftTemplateMeta {
  return {
    fileName: draftTemplate.file.name,
    name: draftTemplate.name,
    size: draftTemplate.file.size,
    lastModified: draftTemplate.file.lastModified,
    tags: draftTemplate.tags,
  }
}

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}

function useDebouncedValue(value: string, delayMs: number) {
  const [debouncedValue, setDebouncedValue] = useState(value)

  useEffect(() => {
    const timeoutId = window.setTimeout(() => setDebouncedValue(value), delayMs)
    return () => window.clearTimeout(timeoutId)
  }, [delayMs, value])

  return debouncedValue
}

function getManualInputType(valueType: string) {
  if (valueType === 'Date') return 'date'
  if (valueType === 'DateTime') return 'datetime-local'
  if (valueType === 'Bool') return 'checkbox'
  if (valueType === 'Int' || valueType === 'Long' || valueType === 'Decimal') return 'number'
  return 'text'
}

export function TemplatesPage() {
  const [restoredPageState] = useState(readPersistedTemplatesPageState)
  const [mode, setMode] = useState<ScreenMode>(restoredPageState.mode)
  const [selectedTemplate, setSelectedTemplate] = useState<TemplateListItemDto | null>(restoredPageState.selectedTemplate)
  const [draftTemplate, setDraftTemplate] = useState<DraftTemplate | null>(null)
  const [draftTemplateMeta, setDraftTemplateMeta] = useState<DraftTemplateMeta | null>(restoredPageState.draftTemplateMeta)
  const [constructorMode, setConstructorMode] = useState<ConstructorMode>(restoredPageState.constructorMode)
  const [constructorTemplateName, setConstructorTemplateName] = useState(restoredPageState.constructorTemplateName)
  const [menuTemplateId, setMenuTemplateId] = useState<string | number | null>(null)
  const [dialog, setDialog] = useState<DialogState>(null)
  const [errorText, setErrorText] = useState<string | null>(null)
  const [generationParams, setGenerationParams] = useState<Record<string, string>>(restoredPageState.generationParams)
  const [isGenerationFormOpen, setIsGenerationFormOpen] = useState(restoredPageState.isGenerationFormOpen)
  const [generatedBlob, setGeneratedBlob] = useState<Blob | null>(null)
  const inputRef = useRef<HTMLInputElement | null>(null)

  useEffect(() => {
    const state: PersistedTemplatesPageState = {
      mode,
      selectedTemplate,
      constructorMode,
      constructorTemplateName,
      generationParams,
      isGenerationFormOpen,
      draftTemplateMeta,
    }

    sessionStorage.setItem(templatesPageStateStorageKey, JSON.stringify(state))
  }, [
    constructorMode,
    constructorTemplateName,
    draftTemplateMeta,
    generationParams,
    isGenerationFormOpen,
    mode,
    selectedTemplate,
  ])

  const templatesQuery = useTemplates()
  const selectedId = selectedTemplate?.id ?? ''
  const detailsQuery = useTemplateDetails(selectedId)
  const scanTags = useScanTemplateForTags()
  const uploadTemplate = useUploadTemplate()
  const updateTemplate = useUpdateTemplate()
  const deleteTemplate = useDeleteTemplate()
  const generateDocument = useGenerateDocument()

  useEffect(() => {
    if (!selectedTemplate || !templatesQuery.data) return

    const freshTemplate = templatesQuery.data.find((template) =>
      sameTemplateId(template.id, selectedTemplate.id),
    )
    let isCancelled = false
    const syncSelection = (action: () => void) => {
      window.queueMicrotask(() => {
        if (!isCancelled) action()
      })
    }

    if (!freshTemplate) {
      syncSelection(() => {
        setSelectedTemplate(null)
        setMode('list')
      })
      return () => {
        isCancelled = true
      }
    }

    if (freshTemplate.name !== selectedTemplate.name) {
      syncSelection(() => setSelectedTemplate(freshTemplate))
      return () => {
        isCancelled = true
      }
    }

    return () => {
      isCancelled = true
    }
  }, [selectedTemplate, templatesQuery.data])

  const selectedConfigurationState = useMemo(
    () => parseConfiguration(detailsQuery.data?.configurationJson),
    [detailsQuery.data?.configurationJson],
  )
  const selectedConfiguration = selectedConfigurationState.configuration
  const isSupportedConfiguration = constructorMode === 'create' || selectedConfigurationState.isSupported
  const generationFormQuery = useGenerationForm(
    selectedId,
    mode === 'details' && isGenerationFormOpen && selectedConfigurationState.isSupported,
  )
  const constructorTags = useMemo(
    () =>
      constructorMode === 'create'
        ? draftTemplate?.tags ?? draftTemplateMeta?.tags ?? []
        : getTagsFromConfiguration(selectedConfiguration),
    [constructorMode, draftTemplate?.tags, draftTemplateMeta?.tags, selectedConfiguration],
  )
  const constructorSessionKey = useMemo(
    () =>
      constructorMode === 'create'
        ? draftTemplate ? getDraftTemplateSessionKey(draftTemplate) : getDraftTemplateMetaSessionKey(draftTemplateMeta)
        : `edit:${selectedTemplate?.id ?? 'none'}:${detailsQuery.data?.configurationJson ?? ''}`,
    [constructorMode, detailsQuery.data?.configurationJson, draftTemplate, draftTemplateMeta, selectedTemplate?.id],
  )
  const generationInputs = generationFormQuery.data?.inputs ?? detailsQuery.data?.generationForm?.inputs ?? []
  const canGenerate = generationInputs.every((input) => !input.required || Boolean(generationParams[input.key]?.trim()))

  const showError = (error: unknown, fallback?: string) => {
    setErrorText(getApiErrorMessage(error, fallback))
  }

  const openDetails = (template: TemplateListItemDto) => {
    setSelectedTemplate(template)
    setMode('details')
    setMenuTemplateId(null)
    setErrorText(null)
    setIsGenerationFormOpen(false)
    setGenerationParams({})
  }

  const openUpload = () => {
    useConstructorStore.getState().reset()
    setDraftTemplate(null)
    setDraftTemplateMeta(null)
    setConstructorMode('create')
    setConstructorTemplateName('')
    setErrorText(null)
    setMode('upload')
  }

  const openConstructorForEdit = (template: TemplateListItemDto) => {
    useConstructorStore.getState().reset()
    setSelectedTemplate(template)
    setConstructorMode('edit')
    setConstructorTemplateName(template.name.replace(/\.docx$/i, ''))
    setMenuTemplateId(null)
    setErrorText(null)
    setMode('constructor')
  }

  const leaveConstructor = () => {
    setErrorText(null)
    if (constructorMode === 'edit') {
      useConstructorStore.getState().reset()
    }
    setMode(constructorMode === 'create' ? 'upload' : 'details')
  }

  const handleFile = async (file: File) => {
    setErrorText(null)
    const name = file.name.replace(/\.docx$/i, '')

    try {
      const result = await scanTags.mutateAsync(file)
      const nextDraftTemplate = { file, name, tags: result.tags }
      setDraftTemplate(nextDraftTemplate)
      setDraftTemplateMeta(createDraftTemplateMeta(nextDraftTemplate))
      setConstructorTemplateName(name)
      setConstructorMode('create')
      setMode('constructor')
    } catch (error) {
      showError(error, 'Не вдалося просканувати документ.')
    }
  }

  const handleCreateTemplate = async (configuration: TemplateConfiguration) => {
    if (!draftTemplate) {
      setErrorText('Після перезавантаження сторінки браузер не відновлює файл шаблону. Оберіть .docx ще раз перед збереженням.')
      return
    }

    setErrorText(null)
    try {
      await uploadTemplate.mutateAsync({
        name: constructorTemplateName.trim() || draftTemplate.name,
        template: draftTemplate.file,
        configurationJson: JSON.stringify(configuration),
      })
      useConstructorStore.getState().reset()
      setDraftTemplate(null)
      setDraftTemplateMeta(null)
      setMode('list')
      setDialog('template-created')
      setErrorText(null)
    } catch (error) {
      showError(error, 'Не вдалося створити шаблон.')
    }
  }

  const handleUpdateTemplate = async (configuration: TemplateConfiguration) => {
    if (!selectedTemplate) return

    setErrorText(null)
    try {
      await updateTemplate.mutateAsync({
        templateId: selectedTemplate.id,
        name: constructorTemplateName.trim() || selectedTemplate.name,
        template: null,
        configurationJson: JSON.stringify(configuration),
      })
      const nextName = constructorTemplateName.trim() || selectedTemplate.name
      useConstructorStore.getState().reset()
      setSelectedTemplate({ ...selectedTemplate, name: nextName })
      setMode('details')
      setDialog('template-updated')
      setErrorText(null)
    } catch (error) {
      showError(error, 'Не вдалося оновити шаблон.')
    }
  }

  const openGenerationForm = () => {
    setErrorText(null)
    if (!selectedConfigurationState.isSupported) {
      setErrorText('Конфігурація шаблону застаріла. Перестворіть шаблон за допомогою конструктора.')
      return
    }
    setIsGenerationFormOpen(true)
    setGenerationParams(
      Object.fromEntries(generationInputs.map((input) => [input.key, generationParams[input.key] ?? ''])),
    )
  }

  const handleTemplateFileRestore = async (file: File) => {
    setErrorText(null)
    const name = file.name.replace(/\.docx$/i, '')

    try {
      const result = await scanTags.mutateAsync(file)
      const nextDraftTemplate = { file, name, tags: result.tags }
      setDraftTemplate(nextDraftTemplate)
      setDraftTemplateMeta(createDraftTemplateMeta(nextDraftTemplate))
      setConstructorTemplateName((currentName) => currentName || name)
    } catch (error) {
      showError(error, 'Не вдалося просканувати документ.')
    }
  }

  const handleGenerate = async () => {
    if (!selectedTemplate) return
    setDialog('generating')
    setErrorText(null)

    try {
      const blob = await generateDocument.mutateAsync({
        id: selectedTemplate.id,
        parameters: generationParams,
      })
      setGeneratedBlob(blob)
      setDialog('document-generated')
    } catch (error) {
      setDialog(null)
      showError(error, 'Не вдалося згенерувати документ.')
    }
  }

  const handleDelete = async () => {
    if (!selectedTemplate) return

    try {
      await deleteTemplate.mutateAsync(selectedTemplate.id)
      setSelectedTemplate(null)
      setMode('list')
      setDialog(null)
      setErrorText(null)
    } catch (error) {
      setDialog(null)
      showError(error, 'Не вдалося видалити шаблон.')
    }
  }

  if (mode === 'constructor') {
    if (constructorMode === 'edit' && detailsQuery.isLoading) {
      return (
        <div className="ui-surface flex h-full min-h-0 items-center justify-center text-[var(--color-primary)]">
          <Loader2 className="mr-2 animate-spin" size={20} />
          Завантаження конфігурації
        </div>
      )
    }

    if (constructorMode === 'edit' && detailsQuery.isError) {
      return (
        <>
          <ErrorMessage message={getApiErrorMessage(detailsQuery.error, 'Не вдалося завантажити конфігурацію.')} onClose={() => setMode('details')} />
          <Button variant="secondary" onClick={() => setMode('details')}>
            Повернутися до шаблону
          </Button>
        </>
      )
    }

    if (constructorMode === 'edit' && !isSupportedConfiguration) {
      return (
        <div className="ui-surface flex min-h-0 flex-col items-start gap-4 p-6">
          <ErrorMessage
            message="Конфігурація шаблону застаріла. Перестворіть шаблон за допомогою конструктора."
            onClose={() => setMode('details')}
          />
          <Button variant="secondary" onClick={() => setMode('details')}>
            Повернутися до шаблону
          </Button>
        </div>
      )
    }

    const documentName =
      constructorMode === 'create'
        ? draftTemplate?.file.name ?? draftTemplateMeta?.fileName ?? 'Новий шаблон.docx'
        : selectedTemplate?.name ?? 'Шаблон.docx'

    return (
      <div className="flex min-h-0 flex-col">
        {errorText && <ErrorMessage message={errorText} onClose={() => setErrorText(null)} />}
        <div className="min-h-0">
          <TemplateConstructor
            documentName={documentName}
            templateName={constructorTemplateName}
            tags={constructorTags}
            initialConfiguration={constructorMode === 'edit' ? selectedConfiguration : undefined}
            sessionKey={constructorSessionKey}
            isSaving={uploadTemplate.isPending || updateTemplate.isPending}
            isTemplateFileMissing={constructorMode === 'create' && Boolean(draftTemplateMeta) && !draftTemplate}
            canBackFromFirstStep={constructorMode === 'create'}
            onTemplateNameChange={setConstructorTemplateName}
            onTemplateFileChange={handleTemplateFileRestore}
            onCancel={leaveConstructor}
            onBack={leaveConstructor}
            onComplete={constructorMode === 'create' ? handleCreateTemplate : handleUpdateTemplate}
          />
        </div>
      </div>
    )
  }

  return (
    <>
      <section className="flex min-h-0 flex-col">
        <div className="mb-[clamp(18px,3vh,28px)] flex shrink-0 items-center justify-between">
          <h1 className="ui-title">
            {mode === 'upload' ? 'Створення нового шаблону' : 'Створені шаблони'}
          </h1>
          {mode !== 'upload' && (
            <Button size="pill" onClick={openUpload}>
              <Plus size={16} />
              Додати шаблон
            </Button>
          )}
        </div>

        {errorText && <ErrorMessage message={errorText} onClose={() => setErrorText(null)} />}
        {templatesQuery.isError && mode === 'list' && (
          <ErrorMessage
            message={getApiErrorMessage(templatesQuery.error, 'Не вдалося завантажити шаблони.')}
            onClose={() => undefined}
          />
        )}
        {detailsQuery.isError && mode === 'details' && (
          <ErrorMessage
            message={getApiErrorMessage(detailsQuery.error, 'Не вдалося завантажити шаблон.')}
            onClose={() => undefined}
          />
        )}

        {mode === 'upload' && (
          <UploadPanel
            draftTemplate={draftTemplate}
            draftTemplateMeta={draftTemplateMeta}
            isScanning={scanTags.isPending}
            inputRef={inputRef}
            onClose={() => {
              setDraftTemplate(null)
              setDraftTemplateMeta(null)
              setMode('list')
            }}
            onContinue={() => {
              if (!draftTemplate && !draftTemplateMeta) return
              if (draftTemplate) {
                setDraftTemplateMeta(createDraftTemplateMeta(draftTemplate))
              }
              setConstructorMode('create')
              setConstructorTemplateName((name) => name || draftTemplate?.name || draftTemplateMeta?.name || '')
              setMode('constructor')
            }}
            onFile={handleFile}
          />
        )}

        {mode === 'details' && selectedTemplate && (
          <div className="ui-surface grid w-full grid-cols-1 gap-[clamp(18px,1.4vw,28px)] overflow-visible px-5 py-[clamp(18px,1.6vw,28px)] lg:grid-cols-[minmax(0,1fr)_minmax(240px,32%)] lg:items-start">
            <div className="min-w-0">
              <button
                className="mb-8 flex items-center gap-3 text-2xl font-extrabold text-[var(--color-primary)] transition hover:text-[var(--color-primary-hover)]"
                onClick={() => setMode('list')}
              >
                <ArrowLeft size={22} />
                {selectedTemplate.name}
              </button>
              <div className="flex w-full max-w-[440px] flex-col gap-4">
                <Button
                  size="lg"
                  className="justify-start"
                  onClick={() => openConstructorForEdit(selectedTemplate)}
                  disabled={detailsQuery.isLoading || !selectedConfigurationState.isSupported}
                >
                  Змінити конфігурацію
                </Button>
                <Button
                  variant="successOutline"
                  size="lg"
                  className="justify-start"
                  onClick={openGenerationForm}
                  disabled={detailsQuery.isLoading || !selectedConfigurationState.isSupported}
                >
                  Згенерувати документ
                </Button>
              </div>

              {!detailsQuery.isLoading && !selectedConfigurationState.isSupported && (
                <div className="mt-5 max-w-[540px] rounded-[var(--radius-ui-sm)] border border-[var(--color-danger)] bg-[var(--color-danger-soft)] p-4 text-sm font-bold text-[var(--color-danger)]">
                  Конфігурація шаблону застаріла. Генерацію заблоковано, перестворіть шаблон за допомогою конструктора.
                </div>
              )}

              {isGenerationFormOpen && detailsQuery.isLoading && (
                <div className="mt-7 flex w-full max-w-[560px] items-center rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-bg-lavender)]/50 p-5 text-sm font-bold text-[var(--color-primary)]">
                  <Loader2 className="mr-2 animate-spin" size={18} />
                  Завантаження шаблону
                </div>
              )}

              {isGenerationFormOpen && !detailsQuery.isLoading && selectedConfigurationState.isSupported && (
                <GenerationFormPanel
                  templateId={selectedTemplate.id}
                  inputs={generationInputs}
                  isLoading={generationFormQuery.isLoading}
                  params={generationParams}
                  onParamsChange={setGenerationParams}
                  onGenerate={() => void handleGenerate()}
                  canGenerate={canGenerate}
                  isGenerating={generateDocument.isPending}
                />
              )}
            </div>
            <LiveJsonPanel value={detailsQuery.data?.configurationJson ?? '{}'} />
          </div>
        )}

        {mode === 'list' && (
          <div className="grid min-h-0 w-full max-w-[1040px] flex-1 content-start gap-[clamp(10px,1vh,16px)] overflow-y-auto overflow-x-hidden pr-1 custom-scrollbar">
            {templatesQuery.isLoading && (
              <div className="flex h-32 items-center justify-center text-[var(--color-primary)]">
                <Loader2 className="mr-2 animate-spin" size={18} />
                Завантаження шаблонів
              </div>
            )}
            {templatesQuery.data?.map((template) => (
              <TemplateRow
                key={template.id}
                template={template}
                isMenuOpen={menuTemplateId === template.id}
                onOpen={() => openDetails(template)}
                onToggleMenu={() => {
                  setSelectedTemplate(template)
                  setMenuTemplateId((current) => (current === template.id ? null : template.id))
                }}
                onGenerate={() => {
                  openDetails(template)
                  setIsGenerationFormOpen(true)
                }}
                onConfigure={() => {
                  openConstructorForEdit(template)
                }}
                onDelete={() => {
                  setSelectedTemplate(template)
                  setMenuTemplateId(null)
                  setDialog('delete')
                }}
              />
            ))}
            {!templatesQuery.isLoading && templatesQuery.data?.length === 0 && (
              <div className="rounded-xl bg-white p-8 text-sm text-slate-500 shadow-sm ring-1 ring-blue-100">
                Шаблонів ще немає. Натисніть “Додати шаблон”, щоб завантажити перший документ.
              </div>
            )}
          </div>
        )}
      </section>

      {dialog === 'generating' && <StatusDialog state="loading" />}
      {dialog === 'template-created' && (
        <StatusDialog state="template-created" onClose={() => setDialog(null)} />
      )}
      {dialog === 'template-updated' && (
        <StatusDialog state="template-updated" onClose={() => setDialog(null)} />
      )}
      {dialog === 'document-generated' && selectedTemplate && (
        <StatusDialog
          state="document-generated"
          onClose={() => setDialog(null)}
          onDownload={() => {
            if (!generatedBlob) return
            downloadBlob(generatedBlob, `${selectedTemplate.name.replace(/\.docx$/i, '')}_result.docx`)
          }}
        />
      )}
      {dialog === 'delete' && selectedTemplate && (
        <DeleteDialog
          templateName={selectedTemplate.name}
          isDeleting={deleteTemplate.isPending}
          onCancel={() => setDialog(null)}
          onConfirm={() => void handleDelete()}
        />
      )}
    </>
  )
}

function ErrorMessage({ message, onClose }: { message: string; onClose: () => void }) {
  return (
    <div className="mb-4 flex items-start justify-between gap-4 whitespace-pre-line rounded-[var(--radius-ui-sm)] border border-[var(--color-danger)] bg-[var(--color-danger-soft)] px-4 py-3 text-sm font-bold text-[var(--color-danger)]">
      <span>{message}</span>
      <button className="text-red-500 hover:text-red-700" onClick={onClose} title="Закрити">
        <X size={16} />
      </button>
    </div>
  )
}

function GenerationFormPanel({
  templateId,
  inputs,
  isLoading,
  params,
  onParamsChange,
  onGenerate,
  canGenerate,
  isGenerating,
}: {
  templateId: TemplateListItemDto['id']
  inputs: GenerationInputDto[]
  isLoading: boolean
  params: Record<string, string>
  onParamsChange: Dispatch<SetStateAction<Record<string, string>>>
  onGenerate: () => void
  canGenerate: boolean
  isGenerating: boolean
}) {
  const dependentKeysByInput = useMemo(() => {
    return inputs.reduce<Record<string, string[]>>((acc, input) => {
      input.dependsOn.forEach((dependency) => {
        acc[dependency] = [...(acc[dependency] ?? []), input.key]
      })
      return acc
    }, {})
  }, [inputs])

  const clearDependents = (key: string, nextParams: Record<string, string>) => {
    dependentKeysByInput[key]?.forEach((dependentKey) => {
      if (nextParams[dependentKey]) nextParams[dependentKey] = ''
      clearDependents(dependentKey, nextParams)
    })
  }

  const updateParam = (key: string, value: string) => {
    onParamsChange((current) => {
      const nextParams = { ...current, [key]: value }
      clearDependents(key, nextParams)
      return nextParams
    })
  }

  return (
    <div className="mt-7 w-full max-w-[560px] rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-bg-lavender)]/50 p-5">
      <h3 className="text-sm font-black uppercase text-blue-700">Параметри генерації</h3>

      {isLoading ? (
        <div className="mt-5 flex items-center text-sm font-bold text-[var(--color-primary)]">
          <Loader2 className="mr-2 animate-spin" size={18} />
          Завантаження форми
        </div>
      ) : (
        <div className="mt-4 space-y-3">
          {inputs.map((input) =>
            input.kind === 'EntitySelect' || input.kind === 'ValueSelect' ? (
              <GenerationEntitySelect
                key={input.key}
                templateId={templateId}
                input={input}
                params={params}
                value={params[input.key] ?? ''}
                onChange={(value) => updateParam(input.key, value)}
              />
            ) : (
              <GenerationManualInput
                key={input.key}
                input={input}
                value={params[input.key] ?? ''}
                onChange={(value) => updateParam(input.key, value)}
              />
            ),
          )}
          {inputs.length === 0 && (
            <p className="text-sm text-slate-500">Цей шаблон не потребує параметрів.</p>
          )}
        </div>
      )}

      <Button
        size="pill"
        className="mt-5 w-full"
        onClick={onGenerate}
        disabled={isLoading || isGenerating || !canGenerate}
      >
        {isGenerating && <Loader2 size={16} className="animate-spin" />}
        Згенерувати документ
      </Button>
    </div>
  )
}

function GenerationManualInput({
  input,
  value,
  onChange,
}: {
  input: GenerationInputDto
  value: string
  onChange: (value: string) => void
}) {
  const type = getManualInputType(input.valueType)

  return (
    <label className="block">
      <span className="mb-1 block text-xs font-bold text-slate-500">
        {input.label}
        {input.required && <span className="text-[var(--color-danger)]"> *</span>}
      </span>
      {type === 'checkbox' ? (
        <input
          type="checkbox"
          checked={value === 'true'}
          onChange={(event) => onChange(event.target.checked ? 'true' : 'false')}
          className="h-5 w-5 accent-[var(--color-primary)]"
        />
      ) : (
        <input
          type={type}
          value={value}
          maxLength={typeof input.maxLength === 'number' ? input.maxLength : undefined}
          onChange={(event) => onChange(event.target.value)}
          className="ui-input w-full px-4 py-3 text-sm font-bold"
          placeholder="Введіть значення"
        />
      )}
    </label>
  )
}

function GenerationEntitySelect({
  templateId,
  input,
  params,
  value,
  onChange,
}: {
  templateId: TemplateListItemDto['id']
  input: GenerationInputDto
  params: Record<string, string>
  value: string
  onChange: (value: string) => void
}) {
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const debouncedQuery = useDebouncedValue(query, 500)
  const dependenciesReady = input.dependsOn.every((dependency) => Boolean(params[dependency]?.trim()))
  const dependencyParams = useMemo(
    () =>
      Object.fromEntries(
        input.dependsOn
          .map((dependency) => [dependency, params[dependency] ?? ''])
          .filter(([, dependencyValue]) => dependencyValue),
      ) as Record<string, string>,
    [input.dependsOn, params],
  )
  const optionsQuery = useQuery({
    queryKey: ['documents', 'generation-input-options', templateId, input.key, debouncedQuery, dependencyParams],
    queryFn: () =>
      fetchGenerationInputOptions({
        templateId,
        inputKey: input.key,
        params: {
          ...dependencyParams,
          ...(debouncedQuery.trim() ? { q: debouncedQuery.trim() } : {}),
          take: '30',
        },
      }),
    enabled: isOpen && dependenciesReady,
  })
  const selectedOption = optionsQuery.data?.items.find((option) => option.value === value)
  const disabled = !dependenciesReady

  return (
    <label className="block">
      <span className="mb-1 block text-xs font-bold text-slate-500">
        {input.label}
        {input.required && <span className="text-[var(--color-danger)]"> *</span>}
      </span>
      <div className="relative">
        <input
          value={isOpen ? query : selectedOption?.label ?? value}
          disabled={disabled}
          onChange={(event) => {
            setQuery(event.target.value)
            setIsOpen(true)
          }}
          onFocus={() => {
            setQuery('')
            setIsOpen(true)
          }}
          onBlur={() => window.setTimeout(() => setIsOpen(false), 140)}
          className="ui-input w-full px-4 py-3 text-sm font-bold disabled:cursor-not-allowed disabled:border-slate-200 disabled:bg-white disabled:text-slate-400"
          placeholder={disabled ? 'Спочатку заповніть залежні поля' : 'Почніть вводити для пошуку'}
        />

        {isOpen && !disabled && (
          <div className="custom-scrollbar absolute left-0 right-0 top-[calc(100%+8px)] z-30 max-h-72 overflow-auto rounded-[var(--radius-ui-sm)] border border-[var(--color-primary)] bg-white p-2 shadow-[var(--shadow-ui-strong)]">
            {optionsQuery.isLoading && (
              <div className="flex items-center px-4 py-3 text-sm font-bold text-[var(--color-primary)]">
                <Loader2 className="mr-2 animate-spin" size={16} />
                Пошук
              </div>
            )}
            {optionsQuery.data?.items.map((option) => (
              <button
                key={option.value}
                type="button"
                className="block w-full rounded-[14px] px-4 py-3 text-left text-sm font-bold text-[var(--color-text)] transition hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-primary)] active:text-white"
                onMouseDown={(event) => {
                  event.preventDefault()
                  onChange(option.value)
                  setQuery('')
                  setIsOpen(false)
                }}
              >
                <span className="block">{option.label}</span>
                {option.description && <span className="mt-1 block text-xs text-slate-400">{option.description}</span>}
              </button>
            ))}
            {optionsQuery.data?.hasMore && (
              <div className="px-4 py-2 text-xs font-bold text-slate-400">
                Уточніть пошук, знайдено більше результатів.
              </div>
            )}
            {!optionsQuery.isLoading && optionsQuery.data?.items.length === 0 && (
              <div className="px-4 py-3 text-sm font-semibold text-slate-400">Нічого не знайдено</div>
            )}
          </div>
        )}
      </div>
    </label>
  )
}

function UploadPanel({
  draftTemplate,
  draftTemplateMeta,
  isScanning,
  inputRef,
  onClose,
  onContinue,
  onFile,
}: {
  draftTemplate: DraftTemplate | null
  draftTemplateMeta: DraftTemplateMeta | null
  isScanning: boolean
  inputRef: React.RefObject<HTMLInputElement | null>
  onClose: () => void
  onContinue: () => void
  onFile: (file: File) => void
}) {
  const handleFileDrop = (event: DragEvent<HTMLButtonElement>) => {
    event.preventDefault()
    event.stopPropagation()

    const file = event.dataTransfer.files?.[0]
    if (file) onFile(file)
  }

  return (
    <div className="ui-surface relative flex min-h-0 flex-1 flex-col overflow-y-auto overflow-x-hidden px-5 py-[clamp(20px,2vw,36px)] custom-scrollbar">
      <button className="absolute right-6 top-6 text-red-500 hover:text-red-600" onClick={onClose} title="Закрити">
        <X size={22} />
      </button>
      <p className="ui-lead mt-4 max-w-3xl">
        Завантажте файл `.docx` з розміченими тегами для початку конфігурації.
      </p>

      <input
        ref={inputRef}
        type="file"
        accept=".docx"
        className="hidden"
        onChange={(event) => {
          const file = event.target.files?.[0]
          if (file) onFile(file)
        }}
      />

      {draftTemplate || draftTemplateMeta ? (
        <div className="mt-[clamp(32px,5vh,56px)] rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-bg-lavender)]/50 p-[clamp(24px,2vw,34px)]">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-[var(--radius-ui-sm)] bg-white text-[var(--color-primary)]">
              <FileText size={24} />
            </div>
            <div>
              <p className="font-extrabold text-[var(--color-primary)]">{draftTemplate?.file.name ?? draftTemplateMeta?.fileName}</p>
              <p className="mt-1 text-sm text-slate-500">Знайдено тегів: {draftTemplate?.tags.length ?? draftTemplateMeta?.tags.length ?? 0}</p>
              {!draftTemplate && draftTemplateMeta && (
                <p className="mt-1 text-xs font-bold text-[var(--color-danger)]">
                  Файл потрібно вибрати повторно перед збереженням.
                </p>
              )}
            </div>
          </div>
          <div className="mt-6 flex gap-3">
            <Button onClick={onContinue}>Продовжити конфігурацію</Button>
            <Button variant="secondary" onClick={() => inputRef.current?.click()}>
              Замінити документ
            </Button>
          </div>
        </div>
      ) : (
        <button
          className="mt-[clamp(32px,5vh,56px)] flex min-h-[clamp(220px,34vh,420px)] flex-1 items-center justify-center rounded-[var(--radius-ui-md)] border border-dashed border-[var(--color-primary)] bg-white text-center transition hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-bg-pink)]"
          onClick={() => inputRef.current?.click()}
          onDragOver={(event) => {
            event.preventDefault()
            event.stopPropagation()
          }}
          onDrop={handleFileDrop}
        >
          <span className="flex flex-col items-center">
            {isScanning ? (
              <Loader2 className="animate-spin text-[var(--color-primary)]" size={42} />
            ) : (
              <UploadCloud className="text-[var(--color-muted)]" size={42} />
            )}
            <span className="mt-6 text-sm font-black text-slate-800">Перетягніть файл сюди або натисніть</span>
            <span className="mt-3 text-xs text-slate-400">Підтримується тільки формат .docx</span>
          </span>
        </button>
      )}
    </div>
  )
}

function TemplateRow({
  template,
  isMenuOpen,
  onOpen,
  onToggleMenu,
  onGenerate,
  onConfigure,
  onDelete,
}: {
  template: TemplateListItemDto
  isMenuOpen: boolean
  onOpen: () => void
  onToggleMenu: () => void
  onGenerate: () => void
  onConfigure: () => void
  onDelete: () => void
}) {
  return (
    <div className="relative min-h-[67px] w-full">
      <div className="relative min-h-[67px] w-[min(72%,760px)] min-w-0">
      <button
        className={cn(
          'flex min-h-[67px] w-full items-center justify-between rounded-[18px] border px-6 py-4 pr-16 text-left text-2xl font-extrabold leading-none shadow-[var(--shadow-ui)] transition',
          isMenuOpen
            ? 'border-[var(--color-primary)] bg-[var(--color-primary)] text-white hover:bg-[var(--color-primary)] active:bg-[var(--color-primary)]'
                    : 'border-transparent bg-white text-[var(--color-primary-soft-text)] hover:bg-[var(--color-primary-hover)] hover:text-white active:bg-[var(--color-primary)] active:text-white',
        )}
        onClick={onOpen}
      >
        <span className="min-w-0 whitespace-normal break-words">{template.name}</span>
      </button>
      <button
        className={cn(
          'absolute right-3 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-[12px] transition hover:bg-white/20 active:bg-white/30',
          isMenuOpen ? 'text-white' : 'text-[var(--color-primary)]',
        )}
        onClick={onToggleMenu}
        title="Дії"
      >
        <MoreVertical size={22} />
      </button>

      {isMenuOpen && (
        <div className="absolute right-0 top-[calc(100%+8px)] z-20 flex w-[min(250px,100%)] flex-col gap-2 rounded-[var(--radius-ui-sm)] border border-[var(--color-bg-lavender)] bg-white p-2 shadow-[var(--shadow-ui-strong)] xl:left-[calc(100%+16px)] xl:right-auto xl:top-0 xl:w-[250px]">
          <button className="rounded-[12px] px-3 py-2 text-center text-xs font-bold text-[var(--color-text)] transition hover:bg-[var(--color-bg-lavender)]" onClick={onGenerate}>
            Згенерувати документ
          </button>
          <button className="rounded-[12px] px-3 py-2 text-center text-xs font-bold text-[var(--color-text)] transition hover:bg-[var(--color-bg-lavender)]" onClick={onConfigure}>
            Змінити конфігурацію
          </button>
          <button className="rounded-[12px] bg-[var(--color-primary)] px-3 py-2 text-center text-xs font-bold text-white transition hover:bg-[var(--color-primary-hover)]" onClick={onDelete}>
            Видалити
          </button>
        </div>
      )}
      </div>
    </div>
  )
}

function LiveJsonPanel({ value }: { value: string }) {
  const formatted = useMemo(() => {
    try {
      return JSON.stringify(JSON.parse(value), null, 2)
    } catch {
      return value
    }
  }, [value])

  return (
    <aside className="ui-json-panel sticky top-4 flex max-h-[calc(100vh-2rem)] max-w-full self-start overflow-hidden p-5">
      <h3 className="text-xs font-extrabold uppercase text-[var(--color-success-soft)]">Live JSON</h3>
      <pre className="json-scrollbar mt-4 max-w-full flex-1 overflow-auto whitespace-pre-wrap break-words font-mono text-[11px] leading-4 text-[var(--color-success-soft)] [overflow-wrap:anywhere]">
        {formatted}
      </pre>
    </aside>
  )
}

function StatusDialog({
  state,
  onClose,
  onDownload,
}: {
  state: 'loading' | 'template-created' | 'template-updated' | 'document-generated'
  onClose?: () => void
  onDownload?: () => void
}) {
  const isLoading = state === 'loading'
  const titleByState = {
    loading: 'Генерація почалася',
    'template-created': 'Шаблон створено!',
    'template-updated': 'Зміни збережено!',
    'document-generated': 'Документ згенеровано!',
  }
  const textByState = {
    loading: 'Зачекайте, система обробляє шаблон та формує документ...',
    'template-created': 'Новий шаблон успішно додано до системи.',
    'template-updated': 'Конфігурацію шаблону успішно оновлено.',
    'document-generated': 'Файл готовий до завантаження.',
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[var(--color-primary-hover)]/35 backdrop-blur-md">
      <div className="flex w-[clamp(380px,28vw,520px)] flex-col items-center rounded-[var(--radius-ui-md)] bg-white px-[clamp(32px,2.5vw,46px)] py-[clamp(36px,4vh,54px)] text-center shadow-[var(--shadow-ui-strong)]">
        {isLoading ? (
          <Loader2 className="animate-spin text-[var(--color-accent)]" size={48} />
        ) : (
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-[var(--color-success-soft)] text-white">
            <CheckCircle2 size={42} />
          </div>
        )}
        <h3 className="mt-5 text-2xl font-extrabold text-[var(--color-primary)]">{titleByState[state]}</h3>
        <p className="mt-3 text-sm font-bold leading-5 text-[var(--color-muted)]">{textByState[state]}</p>
        {state === 'document-generated' && (
          <Button size="pill" className="mt-6 w-full" onClick={onDownload}>
            Завантажити документ (.docx)
          </Button>
        )}
        {!isLoading && (
          <button className="mt-3 rounded-full bg-white px-8 py-3 text-sm font-bold text-[var(--color-primary)] hover:bg-[var(--color-bg-lavender)]" onClick={onClose}>
            Закрити
          </button>
        )}
      </div>
    </div>
  )
}

function DeleteDialog({
  templateName,
  isDeleting,
  onConfirm,
  onCancel,
}: {
  templateName: string
  isDeleting: boolean
  onConfirm: () => void
  onCancel: () => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[var(--color-primary-hover)]/35 backdrop-blur-md">
      <div className="w-[clamp(360px,26vw,500px)] rounded-[var(--radius-ui-md)] bg-white px-[clamp(32px,2.4vw,44px)] py-[clamp(32px,3.6vh,48px)] text-center shadow-[var(--shadow-ui-strong)]">
        <h3 className="text-xl font-black text-red-500">Видалення шаблону</h3>
        <p className="mt-4 text-sm font-medium leading-5 text-slate-500">
          Ви впевнені, що хочете видалити шаблон “{templateName}”?
        </p>
        <Button variant="danger" size="pill" className="mt-6 w-full" onClick={onConfirm} disabled={isDeleting}>
          {isDeleting && <Loader2 size={16} className="animate-spin" />}
          Видалити шаблон
        </Button>
        <button className="mt-3 w-full rounded-full bg-white py-3 text-sm font-bold text-[var(--color-primary)] hover:bg-[var(--color-bg-lavender)]" onClick={onCancel}>
          Скасувати
        </button>
      </div>
    </div>
  )
}
