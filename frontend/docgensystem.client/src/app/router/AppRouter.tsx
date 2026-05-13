import { Navigate, RouterProvider, createBrowserRouter } from 'react-router-dom'
import { TemplatesPage } from '../../pages/templates/TemplatesPage'
import { AppLayout } from '../../widgets/layout/AppLayout'

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/templates" replace /> },
      { path: 'templates', element: <TemplatesPage /> },
      { path: '*', element: <Navigate to="/templates" replace /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
