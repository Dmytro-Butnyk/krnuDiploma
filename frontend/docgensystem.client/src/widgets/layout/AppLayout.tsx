import { LogOut, UserRound } from 'lucide-react'
import { Outlet } from 'react-router-dom'

export function AppLayout() {
  return (
    <div className="flex h-screen flex-col overflow-hidden bg-[#dfeff6] text-slate-950">
      <header className="mx-auto flex w-[min(1680px,calc(100vw-32px))] shrink-0 items-center justify-between pt-[clamp(20px,3vh,38px)] xl:w-[clamp(960px,76vw,1680px)]">
        <div className="h-12 w-12 rounded-xl bg-slate-300/70" />
        <div className="flex items-center gap-4">
          <button
            className="flex h-11 w-11 items-center justify-center rounded-full bg-blue-600 text-white shadow-sm ring-4 ring-white"
            title="Профіль"
          >
            <UserRound size={22} />
          </button>
          <button
            className="flex h-11 w-11 items-center justify-center rounded-full bg-red-100 text-red-500"
            title="Вийти"
          >
            <LogOut size={20} />
          </button>
        </div>
      </header>

      <main className="mx-auto min-h-0 w-[min(1680px,calc(100vw-32px))] flex-1 overflow-hidden pb-[clamp(20px,4vh,44px)] pt-[clamp(20px,3vh,36px)] xl:w-[clamp(960px,76vw,1680px)]">
        <Outlet />
      </main>
    </div>
  )
}
