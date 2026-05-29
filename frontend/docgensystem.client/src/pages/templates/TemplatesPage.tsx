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
import { useEffect, useMemo, useRef, useState } from 'react'
import {
  useDeleteTemplate,
  useGenerateDocument,
  useScanTemplateForTags,
  useTemplateDetails,
  useTemplates,
  useUpdateTemplate,
  useUploadTemplate,
} from '../../entities/template/api/templateApi'
import type { TemplateListItemDto } from '../../entities/template/model/types'
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
type PersistedTemplatesPageState = {
  mode: ScreenMode
  selectedTemplate: TemplateListItemDto | null
  constructorMode: ConstructorMode
  constructorTemplateName: string
  generationParams: Record<string, string>
  isGenerationFormOpen: boolean
}

const templatesPageStateStorageKey = 'templates-page-state'

const defaultTemplatesPageState: PersistedTemplatesPageState = {
  mode: 'list',
  selectedTemplate: null,
  constructorMode: 'create',
  constructorTemplateName: '',
  generationParams: {},
  isGenerationFormOpen: false,
}

function readPersistedTemplatesPageState(): PersistedTemplatesPageState {
  const fallback = defaultTemplatesPageState

  try {
    const raw = sessionStorage.getItem(templatesPageStateStorageKey)
    if (!raw) return fallback

    const parsed = JSON.parse(raw) as Partial<PersistedTemplatesPageState>
    const mode = parsed.mode === 'constructor' && parsed.constructorMode === 'create' ? 'upload' : parsed.mode

    return {
      ...fallback,
      ...parsed,
      mode: mode ?? fallback.mode,
      selectedTemplate: parsed.selectedTemplate ?? null,
      generationParams: parsed.generationParams ?? {},
    }
  } catch {
    return fallback
  }
}

function sameTemplateId(left: TemplateListItemDto['id'], right: TemplateListItemDto['id']) {
  return String(left) === String(right)
}

function parseConfiguration(value?: string | null): TemplateConfiguration | undefined {
  if (!value) return undefined

  try {
    const parsed = JSON.parse(value) as TemplateConfiguration
    if (parsed?.Mapping?.Scalars && parsed?.Mapping?.Tables && Array.isArray(parsed.DataSources)) {
      return parsed
    }
  } catch {
    return undefined
  }

  return undefined
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

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}

export function TemplatesPage() {
  const [restoredPageState] = useState(readPersistedTemplatesPageState)
  const [mode, setMode] = useState<ScreenMode>(restoredPageState.mode)
  const [selectedTemplate, setSelectedTemplate] = useState<TemplateListItemDto | null>(restoredPageState.selectedTemplate)
  const [draftTemplate, setDraftTemplate] = useState<DraftTemplate | null>(null)
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
    const persistedMode = mode === 'constructor' && constructorMode === 'create' ? 'upload' : mode
    const state: PersistedTemplatesPageState = {
      mode: persistedMode,
      selectedTemplate,
      constructorMode,
      constructorTemplateName,
      generationParams,
      isGenerationFormOpen,
    }

    sessionStorage.setItem(templatesPageStateStorageKey, JSON.stringify(state))
  }, [
    constructorMode,
    constructorTemplateName,
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

  const selectedConfiguration = useMemo(
    () => parseConfiguration(detailsQuery.data?.configurationJson),
    [detailsQuery.data?.configurationJson],
  )
  const constructorTags = useMemo(
    () =>
      constructorMode === 'create'
        ? draftTemplate?.tags ?? []
        : getTagsFromConfiguration(selectedConfiguration),
    [constructorMode, draftTemplate?.tags, selectedConfiguration],
  )
  const constructorSessionKey = useMemo(
    () =>
      constructorMode === 'create'
        ? getDraftTemplateSessionKey(draftTemplate)
        : `edit:${selectedTemplate?.id ?? 'none'}:${detailsQuery.data?.configurationJson ?? ''}`,
    [constructorMode, detailsQuery.data?.configurationJson, draftTemplate, selectedTemplate?.id],
  )
  const requiredArguments = detailsQuery.data?.requiredArguments ?? []
  const canGenerate = requiredArguments.every((argument) => generationParams[argument]?.trim())

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
      setDraftTemplate({ file, name, tags: result.tags })
      setConstructorTemplateName(name)
      setConstructorMode('create')
      setMode('constructor')
    } catch (error) {
      showError(error, 'Не вдалося просканувати документ.')
    }
  }

  const handleCreateTemplate = async (configuration: TemplateConfiguration) => {
    if (!draftTemplate) return

    setErrorText(null)
    try {
      await uploadTemplate.mutateAsync({
        name: constructorTemplateName.trim() || draftTemplate.name,
        template: draftTemplate.file,
        configurationJson: JSON.stringify(configuration),
      })
      useConstructorStore.getState().reset()
      setDraftTemplate(null)
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
    setIsGenerationFormOpen(true)
    setGenerationParams(
      Object.fromEntries(requiredArguments.map((argument) => [argument, generationParams[argument] ?? ''])),
    )
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

    const documentName =
      constructorMode === 'create'
        ? draftTemplate?.file.name ?? 'Новий шаблон.docx'
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
            canBackFromFirstStep={constructorMode === 'create'}
            onTemplateNameChange={setConstructorTemplateName}
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
            isScanning={scanTags.isPending}
            inputRef={inputRef}
            onClose={() => {
              setDraftTemplate(null)
              setMode('list')
            }}
            onContinue={() => {
              if (!draftTemplate) return
              setConstructorMode('create')
              setConstructorTemplateName((name) => name || draftTemplate.name)
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
                  disabled={detailsQuery.isLoading}
                >
                  Змінити конфігурацію
                </Button>
                <Button variant="successOutline" size="lg" className="justify-start" onClick={openGenerationForm}>
                  Згенерувати документ
                </Button>
              </div>

              {isGenerationFormOpen && (
                <div className="mt-7 w-full max-w-[540px] rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-bg-lavender)]/50 p-5">
                  <h3 className="text-sm font-black uppercase text-blue-700">Параметри генерації</h3>
                  <div className="mt-4 space-y-3">
                    {requiredArguments.map((argument) => (
                      <label key={argument} className="block">
                        <span className="mb-1 block text-xs font-bold text-slate-500">{argument}</span>
                        <input
                          value={generationParams[argument] ?? ''}
                          onChange={(event) =>
                            setGenerationParams((params) => ({ ...params, [argument]: event.target.value }))
                          }
                          className="ui-input w-full px-4 py-3 text-sm font-bold"
                          placeholder="Введіть значення"
                        />
                      </label>
                    ))}
                    {requiredArguments.length === 0 && (
                      <p className="text-sm text-slate-500">Цей шаблон не потребує параметрів.</p>
                    )}
                  </div>
                  <Button
                    size="pill"
                    className="mt-5 w-full"
                    onClick={() => void handleGenerate()}
                    disabled={requiredArguments.length > 0 && !canGenerate}
                  >
                    Згенерувати документ
                  </Button>
                </div>
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

function UploadPanel({
  draftTemplate,
  isScanning,
  inputRef,
  onClose,
  onContinue,
  onFile,
}: {
  draftTemplate: DraftTemplate | null
  isScanning: boolean
  inputRef: React.RefObject<HTMLInputElement | null>
  onClose: () => void
  onContinue: () => void
  onFile: (file: File) => void
}) {
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

      {draftTemplate ? (
        <div className="mt-[clamp(32px,5vh,56px)] rounded-[var(--radius-ui-md)] border border-[var(--color-bg-lavender)] bg-[var(--color-bg-lavender)]/50 p-[clamp(24px,2vw,34px)]">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-[var(--radius-ui-sm)] bg-white text-[var(--color-primary)]">
              <FileText size={24} />
            </div>
            <div>
              <p className="font-extrabold text-[var(--color-primary)]">{draftTemplate.file.name}</p>
              <p className="mt-1 text-sm text-slate-500">Знайдено тегів: {draftTemplate.tags.length}</p>
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
    <aside className="ui-json-panel flex h-fit max-w-full self-start flex-col p-5">
      <h3 className="text-xs font-extrabold uppercase text-[var(--color-success-soft)]">Live JSON</h3>
      <pre className="json-scrollbar mt-4 max-w-full overflow-x-auto overflow-y-visible whitespace-pre-wrap break-words font-mono text-[11px] leading-4 text-[var(--color-success-soft)] [overflow-wrap:anywhere]">
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
