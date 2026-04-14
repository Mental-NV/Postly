import { AppProviders } from './app/providers/AppProviders'
import { AppRoutes } from './app/routes'

export function App(): React.JSX.Element {
  return (
    <AppProviders>
      <AppRoutes />
    </AppProviders>
  )
}
