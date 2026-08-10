import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { CustomersPage } from './features/customers/CustomersPage'
import { CustomerDetailPage } from './features/customers/CustomerDetailPage'
import { CustomerFormPage } from './features/customers/CustomerFormPage'
import { EstimatesListPage } from './features/estimates/EstimatesListPage'
import { EstimateBuilderPage } from './features/estimates/EstimateBuilderPage'

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <Navigate to="/customers" replace /> },
          { path: "customers", element: <CustomersPage /> },
          { path: "customers/new", element: <CustomerFormPage /> },
          { path: "customers/:id", element: <CustomerDetailPage /> },
          { path: "customers/:id/edit", element: <CustomerFormPage /> },
          { path: "estimates", element: <EstimatesListPage /> },
          { path: "estimates/new", element: <EstimateBuilderPage /> },
          { path: "estimates/:id/edit", element: <EstimateBuilderPage /> },
        ],
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);