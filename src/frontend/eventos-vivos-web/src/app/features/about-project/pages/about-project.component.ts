import { Component } from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-about-project',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule],
  template: `
    <div class="page-container about-page">
      <div class="page-header">
        <div>
          <h1>Acerca del Proyecto</h1>
          <p class="page-subtitle">Resumen técnico de EventosVivos para Ceiba Software</p>
        </div>
      </div>

      <mat-card class="hero-card" appearance="outlined">
        <mat-card-content>
          <div class="hero-content">
            <div>
              <p class="eyebrow">Prueba técnica Full Stack .NET + Angular</p>
              <h2>EventosVivos</h2>
              <p class="hero-subtitle">Ceiba Software</p>
              <p>
                EventosVivos es un sistema de gestión de eventos y reservas desarrollado como parte del proceso técnico
                para el cargo de Desarrollador Full Stack .NET + Angular.
              </p>
              <p>
                La solución cubre administración de eventos, lugares, reservas, pagos y reportes de ocupación, con foco
                en mantenibilidad, separación de responsabilidades, pruebas automatizadas y ejecución mediante
                contenedores.
              </p>
            </div>
            <div class="hero-summary">
              <span><strong>Backend:</strong> .NET 10 + SQL Server</span>
              <span><strong>Frontend:</strong> Angular 22</span>
              <span><strong>Arquitectura:</strong> Clean / Onion + CQRS ligero</span>
              <span><strong>Infraestructura:</strong> Azure + ACR + Container Apps + Azure SQL Database</span>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="section-card resources-card" appearance="outlined">
        <mat-card-header>
          <mat-icon mat-card-avatar>link</mat-icon>
          <mat-card-title>Recursos</mat-card-title>
          <mat-card-subtitle>Enlaces relevantes del proyecto</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <div class="resource-links">
            <a mat-stroked-button href="https://github.com/CaportilloC/eventos-vivos" target="_blank" rel="noopener noreferrer">
              <mat-icon>code</mat-icon>
              Repositorio GitHub
            </a>
            <a mat-stroked-button href="https://www.linkedin.com/in/christian-alexander-portillo" target="_blank" rel="noopener noreferrer">
              <mat-icon>person</mat-icon>
              LinkedIn
            </a>
            <a mat-stroked-button [href]="swaggerUrl" target="_blank" rel="noopener noreferrer">
              <mat-icon>api</mat-icon>
              Swagger API
            </a>
            <a mat-stroked-button href="mailto:wesker980@gmail.com">
              <mat-icon>mail</mat-icon>
              wesker980@gmail.com
            </a>
            <a mat-stroked-button href="tel:+573137021105">
              <mat-icon>call</mat-icon>
              +57 3137021105
            </a>
            <a mat-stroked-button href="https://wa.me/573137021105" target="_blank" rel="noopener noreferrer">
              <mat-icon>chat</mat-icon>
              WhatsApp
            </a>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="section-card" appearance="outlined">
        <mat-card-header>
          <mat-icon mat-card-avatar>schema</mat-icon>
          <mat-card-title>Decisiones Arquitectónicas</mat-card-title>
          <mat-card-subtitle>Diseño proporcional al alcance real de la prueba</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <div class="chip-list" aria-label="Decisiones arquitectónicas principales">
            @for (item of architectureItems; track item) {
              <span class="tech-chip">{{ item }}</span>
            }
          </div>
          <p>
            La arquitectura fue seleccionada considerando mantenibilidad, facilidad de pruebas, despliegue simple y
            evolución futura. Se optó por un Modular Monolith basado en Clean Architecture y Onion Architecture,
            complementado con CQRS ligero mediante MediatR.
          </p>
          <p>
            La decisión fue intencional: microservicios, event-driven architecture o CQRS avanzado habrían agregado
            complejidad operativa innecesaria para el dominio planteado. La simplicidad de la solución responde a
            criterio arquitectónico, no a una limitación técnica.
          </p>
          <div class="value-list" aria-label="Criterios maximizados por la arquitectura">
            @for (item of architectureGoals; track item) {
              <span><mat-icon>check_circle</mat-icon>{{ item }}</span>
            }
          </div>
        </mat-card-content>
      </mat-card>

      <div class="section-grid">
        <mat-card class="section-card" appearance="outlined">
          <mat-card-header>
            <mat-icon mat-card-avatar>dns</mat-icon>
            <mat-card-title>Backend</mat-card-title>
            <mat-card-subtitle>API, casos de uso y persistencia</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="chip-list" aria-label="Tecnologías backend">
              @for (technology of backendTechnologies; track technology) {
                <span class="tech-chip">{{ technology }}</span>
              }
            </div>
            <p>
              .NET 10 fue seleccionado por ser una plataforma moderna para APIs empresariales. El backend implementa
              validaciones con FluentValidation, CQRS ligero con MediatR y persistencia con Entity Framework Core sobre
              SQL Server.
            </p>
          </mat-card-content>
        </mat-card>

        <mat-card class="section-card" appearance="outlined">
          <mat-card-header>
            <mat-icon mat-card-avatar>web</mat-icon>
            <mat-card-title>Frontend</mat-card-title>
            <mat-card-subtitle>SPA empresarial simple y mantenible</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="chip-list" aria-label="Tecnologías frontend">
              @for (technology of frontendTechnologies; track technology) {
                <span class="tech-chip">{{ technology }}</span>
              }
            </div>
            <p>
              Angular fue seleccionado por su estructura, tipado fuerte con TypeScript e integración natural con APIs
              REST. Angular Material aporta consistencia visual y RxJS facilita el manejo reactivo de datos y eventos.
            </p>
          </mat-card-content>
        </mat-card>

        <mat-card class="section-card" appearance="outlined">
          <mat-card-header>
            <mat-icon mat-card-avatar>storage</mat-icon>
            <mat-card-title>Persistencia</mat-card-title>
            <mat-card-subtitle>Modelo relacional versionado</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="chip-list" aria-label="Tecnologías de persistencia">
              @for (technology of persistenceTechnologies; track technology) {
                <span class="tech-chip">{{ technology }}</span>
              }
            </div>
            <p>
              SQL Server fue seleccionado por su robustez y adopción empresarial. La persistencia usa migraciones
              versionadas y configuraciones explícitas del modelo de datos mediante Entity Framework Core.
            </p>
          </mat-card-content>
        </mat-card>
      </div>

      <mat-card class="section-card" appearance="outlined">
        <mat-card-header>
          <mat-icon mat-card-avatar>verified</mat-icon>
          <mat-card-title>Calidad y Pruebas</mat-card-title>
          <mat-card-subtitle>Validación automatizada de reglas y flujos críticos</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>
            La solución incorpora pruebas automatizadas para validar reglas de negocio, flujos funcionales y estabilidad
            general. Esto reduce regresiones y deja documentado el comportamiento esperado del sistema.
          </p>
          <div class="test-results prominent-results">
            <span>Backend Tests: <strong>105 / 105 exitosos</strong></span>
            <span>Frontend Tests: <strong>10 / 10 exitosos</strong></span>
          </div>
          <div class="chip-list" aria-label="Escenarios cubiertos por pruebas">
            @for (scenario of testScenarios; track scenario) {
              <span class="tech-chip">{{ scenario }}</span>
            }
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="section-card" appearance="outlined">
        <mat-card-header>
          <mat-icon mat-card-avatar>rocket_launch</mat-icon>
          <mat-card-title>Entrega y Operación</mat-card-title>
          <mat-card-subtitle>Requerimientos, contenedores y preparación cloud</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>
            Todos los requerimientos técnicos fueron considerados durante el diseño: API RESTful, Angular consumiendo la
            API, base relacional, pruebas automatizadas, arquitectura documentada, Docker, repositorio público y
            preparación para despliegue cloud.
          </p>
          <div class="requirement-grid" aria-label="Requerimientos técnicos cumplidos">
            @for (requirement of technicalRequirements; track requirement) {
              <span><mat-icon>task_alt</mat-icon>{{ requirement }}</span>
            }
          </div>
          <div class="operation-grid">
            <div>
              <h3>Contenerización</h3>
              <p>
                Backend ASP.NET Core, frontend Angular y SQL Server fueron preparados para ejecutarse con Docker Compose,
                favoreciendo reproducibilidad, consistencia entre entornos y simplicidad de instalación.
              </p>
              <div class="chip-list" aria-label="Elementos de contenerización">
                @for (item of containerizationItems; track item) {
                  <span class="tech-chip">{{ item }}</span>
                }
              </div>
            </div>
            <div>
              <h3>Infraestructura Cloud</h3>
              <p>
                La estrategia contempla Azure Container Registry, Azure Container Apps para frontend/backend y Azure SQL
                Database, permitiendo evolución futura sin cambios significativos en el código de aplicación.
              </p>
              <div class="chip-list" aria-label="Servicios Azure contemplados">
                @for (technology of cloudTechnologies; track technology) {
                  <span class="tech-chip">{{ technology }}</span>
                }
              </div>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="section-card" appearance="outlined">
        <mat-card-header>
          <mat-icon mat-card-avatar>sync_alt</mat-icon>
          <mat-card-title>Automatización y CI/CD</mat-card-title>
          <mat-card-subtitle>Integración, validación y despliegue controlado</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>
            El repositorio implementa CI/CD con GitHub Actions para validar cambios antes de integrarlos y desplegar la
            plataforma en Azure de forma controlada.
          </p>
          <p>
            El flujo ejecuta pruebas de backend y frontend, valida builds Docker, publica imágenes en Azure Container
            Registry y actualiza Azure Container Apps usando autenticación OIDC, sin almacenar credenciales largas en el
            repositorio.
          </p>
          <div class="chip-list" aria-label="Herramientas de CI/CD">
            @for (tool of cicdTechnologies; track tool) {
              <span class="tech-chip">{{ tool }}</span>
            }
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="section-card" appearance="outlined">
        <mat-card-header>
          <mat-icon mat-card-avatar>terminal</mat-icon>
          <mat-card-title>Entorno y Herramientas</mat-card-title>
          <mat-card-subtitle>Productividad, automatización y apoyo al criterio técnico</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <div class="chip-list" aria-label="Entorno de desarrollo">
            @for (tool of developmentEnvironment; track tool) {
              <span class="tech-chip">{{ tool }}</span>
            }
          </div>
          <p>
            La solución fue desarrollada principalmente sobre Ubuntu 24.04 LTS. También se utilizaron herramientas de
            asistencia basadas en IA como apoyo para análisis, diseño, documentación y validación técnica, sin sustituir
            el criterio profesional ni las decisiones arquitectónicas.
          </p>
          <div class="chip-list" aria-label="Herramientas de asistencia utilizadas">
            @for (tool of aiAssistanceTools; track tool) {
              <span class="tech-chip subtle-chip">{{ tool }}</span>
            }
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="thanks-card" appearance="outlined">
        <mat-card-header>
          <mat-icon mat-card-avatar>volunteer_activism</mat-icon>
          <mat-card-title>Agradecimiento</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>Muchas gracias a Ceiba Software por la oportunidad de presentar esta solución.</p>
          <p>
            El proyecto fue desarrollado aplicando principios de arquitectura limpia, buenas prácticas de ingeniería de
            software, pruebas automatizadas, contenerización y preparación para despliegues cloud.
          </p>
          <p>
            Más allá del cumplimiento funcional, el objetivo fue construir una solución mantenible, escalable y alineada
            con estándares utilizados en proyectos empresariales modernos.
          </p>
          <p>
            Espero que el resultado refleje adecuadamente mis capacidades técnicas y criterio de diseño como desarrollador
            Full Stack.
          </p>
          <p class="signature">Christian Alexander Portillo</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: `
    .about-page {
      display: grid;
      gap: 24px;
    }

    .hero-card,
    .section-card,
    .thanks-card {
      border-radius: var(--ev-radius-md);
    }

    .hero-content {
      display: grid;
      grid-template-columns: minmax(0, 1fr) minmax(260px, 360px);
      gap: 24px;
      align-items: center;
    }

    .hero-content h2 {
      margin: 4px 0 10px;
      font-size: 28px;
      color: var(--ev-text-primary);
    }

    .hero-subtitle {
      margin-top: -4px;
      color: var(--ev-text-primary) !important;
      font-weight: 700;
    }

    .hero-content p,
    .section-card p,
    .thanks-card p {
      color: var(--ev-text-secondary);
      font-size: 13px;
      line-height: 1.6;
    }

    .eyebrow {
      margin: 0;
      color: var(--ev-primary) !important;
      font-weight: 700;
      letter-spacing: 0.04em;
      text-transform: uppercase;
    }

    .hero-summary {
      display: grid;
      gap: 10px;
      padding: 16px;
      border-radius: var(--ev-radius-md);
      background: #eef2ff;
      color: #1e3a8a;
      font-size: 13px;
      line-height: 1.4;
    }

    .section-grid,
    .operation-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 16px;
    }

    .operation-grid {
      margin-top: 18px;
    }

    .operation-grid h3 {
      margin: 0 0 8px;
      color: var(--ev-text-primary);
      font-size: 14px;
      font-weight: 700;
    }

    .section-card mat-card-title,
    .thanks-card mat-card-title {
      font-size: 16px;
      font-weight: 600;
    }

    .chip-list {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 14px;
    }

    .tech-chip {
      display: inline-flex;
      align-items: center;
      min-height: 28px;
      padding: 4px 10px;
      border-radius: 999px;
      background: #f1f5f9;
      color: #334155;
      font-size: 12px;
      font-weight: 600;
    }

    .test-results {
      display: grid;
      gap: 8px;
      margin: 14px 0;
      padding: 12px;
      border-radius: var(--ev-radius-sm);
      background: #ecfdf5;
      color: #166534;
      font-size: 14px;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    }

    .requirement-grid,
    .value-list {
      display: grid;
      gap: 10px;
      margin-top: 14px;
    }

    .requirement-grid {
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    }

    .value-list {
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    }

    .requirement-grid span,
    .value-list span {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      color: var(--ev-text-secondary);
      font-size: 13px;
      line-height: 1.4;
    }

    .requirement-grid mat-icon,
    .value-list mat-icon {
      color: #16a34a;
      font-size: 18px;
      width: 18px;
      height: 18px;
      line-height: 18px;
      flex-shrink: 0;
    }

    .subtle-chip {
      background: #eef2ff;
      color: #3730a3;
    }

    .resource-links {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }

    .resource-links a {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .thanks-card {
      border-color: #bfdbfe;
      background: linear-gradient(135deg, #eff6ff 0%, #ffffff 100%);
    }

    .thanks-card p {
      margin: 0 0 10px;
    }

    .thanks-card .signature {
      margin-bottom: 0;
      color: var(--ev-text-primary);
      font-weight: 700;
    }

    @media (max-width: 768px) {
      .hero-content {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 480px) {
      .resource-links {
        flex-direction: column;
      }
    }
  `,
})
export class AboutProjectComponent {
  protected readonly architectureItems = [
    'Modular Monolith',
    'Clean Architecture',
    'Onion Architecture',
    'CQRS ligero',
    'MediatR',
  ];
  protected readonly architectureGoals = [
    'Separación de responsabilidades',
    'Mantenibilidad',
    'Testabilidad',
    'Facilidad de despliegue',
    'Evolución futura',
  ];
  protected readonly technicalRequirements = [
    'API RESTful',
    'Angular consumiendo API',
    'Pruebas automatizadas',
    'Arquitectura documentada',
    'Docker',
    'Base de datos relacional',
    'Preparación cloud',
    'Repositorio público',
  ];
  protected readonly backendTechnologies = [
    '.NET 10',
    'ASP.NET Core Web API',
    'MediatR',
    'FluentValidation',
    'Entity Framework Core',
    'SQL Server',
  ];
  protected readonly frontendTechnologies = ['Angular 22', 'Angular Material', 'TypeScript', 'RxJS', 'SweetAlert2'];
  protected readonly persistenceTechnologies = ['SQL Server', 'Entity Framework Core', 'Migrations', 'Docker'];
  protected readonly testScenarios = [
    'Creación de eventos',
    'Restricciones de capacidad',
    'Superposición de horarios',
    'Reservas',
    'Confirmación de pagos',
    'Cancelaciones',
    'Reportes de ocupación',
    'Casos borde de negocio',
  ];
  protected readonly developmentEnvironment = [
    'Ubuntu 24.04 LTS',
    '.NET 10',
    'Angular 22',
    'Docker',
    'SQL Server',
    'GitHub',
    'Azure',
  ];
  protected readonly aiAssistanceTools = ['OpenCode', 'Gentle AI', 'Spec Kit', 'ChatGPT'];
  protected readonly containerizationItems = [
    'Docker',
    'Docker Compose',
    'Backend Container',
    'Frontend Container',
    'SQL Server Container',
  ];
  protected readonly cloudTechnologies = [
    'Azure',
    'Azure Container Registry',
    'Azure Container Apps',
    'Azure SQL Database',
  ];
  protected readonly cicdTechnologies = [
    'GitHub Actions',
    'Azure OIDC',
    'Docker Build',
    'ACR Push',
    'Container Apps Deploy',
    'Smoke Tests',
  ];
  protected readonly swaggerUrl = this.buildSwaggerUrl();

  private buildSwaggerUrl(): string {
    if (environment.apiBaseUrl.startsWith('/')) {
      return '/swagger';
    }

    return environment.apiBaseUrl.replace(/\/api\/v1\/?$/, '/swagger');
  }
}
