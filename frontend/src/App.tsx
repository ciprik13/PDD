import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "@/context/AuthContext";
import { ProtectedRoute, AdminRoute } from "@/components/auth/ProtectedRoute";
import { LoginPage } from "@/pages/Login";
import { Sidebar } from "@/components/layout/Sidebar";
import { DashboardPage } from "@/pages/Dashboard";
import { FieldReportPage } from "@/pages/FieldReport";
import { DetectionLogPage } from "@/pages/DetectionLog";
import { LiveCameraPage } from "@/pages/LiveCamera";
import { PlantsPage } from "@/pages/Plants";
import { PlantPassportPage } from "@/pages/PlantPassport";
import { TreatmentsPage } from "@/pages/Treatments";
import { SeverityTrendsPage } from "@/pages/SeverityTrends";
import { StandPositionPage } from "@/pages/StandPosition";
import { AlertsPage } from "@/pages/Alerts";
import { TreatmentHistoryPage } from "@/pages/TreatmentHistory";
import { AdminApiKeysPage } from "@/pages/AdminApiKeys";
import styles from "./App.module.css";

function AppShell() {
  return (
    <div className={styles.shell}>
      <Sidebar />
      <main className={styles.content}>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/field-report" element={<FieldReportPage />} />
          <Route path="/camera"     element={<LiveCameraPage />} />
          <Route path="/detections" element={<DetectionLogPage />} />
          <Route path="/plants"     element={<PlantsPage />} />
          <Route path="/passport"   element={<PlantPassportPage />} />
          <Route path="/trends"     element={<SeverityTrendsPage />} />
          <Route path="/treatments" element={<TreatmentsPage />} />
          <Route path="/history"    element={<TreatmentHistoryPage />} />
          <Route path="/position"   element={<StandPositionPage />} />
          <Route path="/alerts"     element={<AlertsPage />} />
          <Route path="/admin/api-keys" element={<AdminRoute><AdminApiKeysPage /></AdminRoute>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <AppShell />
              </ProtectedRoute>
            }
          />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
