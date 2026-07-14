import 'zone.js/node';
import { ngExpressEngine } from '@nguniversal/express-engine';
import * as express from 'express';
import { join } from 'path';

import { AppServerModule } from './app/app.server.module';

const app = express();
const distFolder = join(process.cwd(), 'dist');
const indexHtml = 'index.html';

app.engine('html', ngExpressEngine({ bootstrap: AppServerModule }));
app.set('view engine', 'html');
app.set('views', distFolder);

app.get('*.*', express.static(distFolder));

app.get('*', (req, res) => {
  res.render(indexHtml, { req });
});

const port = process.env.PORT || 4000;
app.listen(port, () => {
  console.log(`Node Express server listening on http://localhost:${port}`);
});
