import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));


window.addEventListener('load', () => {
  const loader = document.getElementById('loader');

  if (loader) {
    loader.style.transition = 'opacity 0.5s ease';
    loader.style.opacity = '0';

    setTimeout(() => {
      loader.style.display = 'none';

      // 👇 Aquí activás la app
      document.body.classList.remove('loading');
      document.body.classList.add('loaded');

    }, 1000);
  }
});