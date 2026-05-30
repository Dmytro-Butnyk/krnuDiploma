import { ArrowLeft, CheckCircle2, Loader2 } from 'lucide-react'
import { useRef, type DragEvent } from 'react'
import { Button } from '../../../../shared/ui/Button'

type Props = {
  documentName: string
  templateName: string
  validationErrors: string[]
  isTemplateFileMissing?: boolean
  onTemplateNameChange: (name: string) => void
  onTemplateFileChange?: (file: File) => void
  onBack: () => void
  onSave: () => void
  isSaving: boolean
  showDocumentBackButton?: boolean
}

export function ReviewStep({
  documentName,
  templateName,
  validationErrors,
  isTemplateFileMissing = false,
  onTemplateNameChange,
  onTemplateFileChange,
  onBack,
  onSave,
  isSaving,
  showDocumentBackButton = true,
}: Props) {
  const inputRef = useRef<HTMLInputElement | null>(null)
  const handleTemplateFile = (file: File | undefined) => {
    if (file && onTemplateFileChange) onTemplateFileChange(file)
  }
  const handleTemplateFileDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault()
    event.stopPropagation()
    handleTemplateFile(event.dataTransfer.files?.[0])
  }

  return (
    <div className="custom-scrollbar flex h-full min-h-0 w-full flex-col items-center overflow-y-auto overflow-x-hidden px-1 pb-6">
      {showDocumentBackButton && (
        <Button variant="secondary" size="sm" className="max-w-full self-start" onClick={onBack}>
          <ArrowLeft size={13} />
          Повернутися до документу
        </Button>
      )}

      <div className="mt-[clamp(28px,8vh,96px)] flex w-full max-w-[460px] shrink-0 flex-col items-center text-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-[var(--color-success-soft)] text-white">
          <CheckCircle2 size={40} />
        </div>
        <h3 className="ui-step-title mt-5">Конфігурація успішна!</h3>
        <div className="mt-4 max-w-full overflow-hidden text-ellipsis whitespace-nowrap rounded-full border border-[var(--color-primary)] bg-white px-5 py-2 text-sm font-bold text-[var(--color-primary)]">
          {documentName}
        </div>

        <label className="mt-8 block w-full text-left">
          <span className="ui-label mb-2 block">Назва шаблону в системі</span>
          <input
            className="ui-input w-full px-4 py-3 text-base font-bold"
            value={templateName}
            onChange={(event) => onTemplateNameChange(event.target.value)}
            placeholder={documentName.replace(/\.docx$/i, '')}
          />
        </label>

        {isTemplateFileMissing && (
          <div
            className="mt-5 w-full rounded-[var(--radius-ui-sm)] border border-[var(--color-danger)] bg-[var(--color-danger-soft)] p-4 text-left text-sm font-bold text-[var(--color-danger)]"
            onDragOver={(event) => {
              event.preventDefault()
              event.stopPropagation()
            }}
            onDrop={handleTemplateFileDrop}
          >
            <p>Після перезавантаження сторінки браузер не відновлює файл шаблону. Оберіть .docx ще раз перед збереженням.</p>
            <input
              ref={inputRef}
              type="file"
              accept=".docx"
              className="hidden"
              onChange={(event) => {
                handleTemplateFile(event.target.files?.[0])
              }}
            />
            <Button
              variant="secondary"
              size="sm"
              className="mt-3"
              onClick={() => inputRef.current?.click()}
            >
              Обрати .docx
            </Button>
          </div>
        )}

        <Button size="pill" className="mt-6 w-64 max-w-full" onClick={onSave} disabled={isSaving || isTemplateFileMissing}>
          {isSaving && <Loader2 size={18} className="animate-spin" />}
          Зберегти
        </Button>

        {validationErrors.length > 0 && (
          <div className="mt-5 w-full rounded-[var(--radius-ui-sm)] border border-[var(--color-danger)] bg-[var(--color-danger-soft)] p-4 text-left text-sm font-bold text-[var(--color-danger)]">
            <p>Виправте помилки перед збереженням:</p>
            <ul className="mt-2 list-disc space-y-1 pl-5">
              {validationErrors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </div>
  )
}
