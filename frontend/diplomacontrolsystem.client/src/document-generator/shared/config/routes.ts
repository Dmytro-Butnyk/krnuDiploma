import { FileCog, FileText, WandSparkles } from 'lucide-react'

export const navigationItems = [
  {
    label: 'Конструктор',
    path: '/constructor',
    icon: FileCog,
  },
  {
    label: 'Шаблоны',
    path: '/templates',
    icon: FileText,
  },
  {
    label: 'Генерация',
    path: '/generate',
    icon: WandSparkles,
  },
] as const
