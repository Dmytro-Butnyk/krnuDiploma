import { ExternalLink } from 'lucide-react'
import { useEffect } from 'react'
import { generatorUrl } from '../../shared/config/externalLinks'

export function GeneratorRedirectPage() {
  useEffect(() => {
    window.location.assign(generatorUrl)
  }, [])

  return (
    <section className="grid min-h-[520px] place-items-center">
      <a
        href={generatorUrl}
        className="inline-flex items-center gap-3 rounded-full bg-blue-600 px-10 py-5 text-2xl font-bold text-white shadow-sm transition hover:bg-blue-700"
      >
        Перейти до генератора
        <ExternalLink size={28} />
      </a>
    </section>
  )
}
