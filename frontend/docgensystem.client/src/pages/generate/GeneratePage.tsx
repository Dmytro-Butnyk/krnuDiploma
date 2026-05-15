import { WandSparkles } from 'lucide-react'
import { Button } from '../../shared/ui/Button'

export function GeneratePage() {
  return (
    <section className="h-full min-h-0 overflow-y-auto overflow-x-hidden rounded-xl border border-slate-200 bg-white p-[clamp(24px,2vw,36px)] shadow-sm custom-scrollbar">
      <div className="flex w-full max-w-[920px] flex-wrap items-start gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
          <WandSparkles size={22} />
        </div>
        <div>
          <h2 className="text-xl font-black text-slate-950">Генерация документа</h2>
          <p className="mt-2 text-sm leading-6 text-slate-500">
            Этот раздел подготовлен под `/api/documents/{'{id}'}/generate`: выбор шаблона, заполнение
            обязательных аргументов и скачивание DOCX.
          </p>
          <Button className="mt-5">Выбрать шаблон</Button>
        </div>
      </div>
    </section>
  )
}
