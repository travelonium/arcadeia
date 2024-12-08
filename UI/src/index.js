/* 
 *  Copyright © 2024 Travelonium AB
 *  
 *  This file is part of Arcadeia.
 *  
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *  
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *  
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *  
 */

import App from './App';
import React from 'react';
import { Provider } from 'react-redux';
import { store, persistor } from './store';
import { createRoot } from 'react-dom/client';
import { ToastContainer } from 'react-toastify';
import { BrowserRouter } from 'react-router';
import { PersistGate } from 'redux-persist/integration/react';
import * as serviceWorkerRegistration from './serviceWorkerRegistration';
import reportWebVitals from './reportWebVitals';
import './stylesheet.scss';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');
const root = createRoot(rootElement);

root.render(
    <BrowserRouter basename={baseUrl}>
        <Provider store={store}>
            <PersistGate loading={null} persistor={persistor}>
                <App />
            </PersistGate>
        </Provider>
        <ToastContainer position="bottom-right" theme="colored" />
    </BrowserRouter>
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://cra.link/PWA
serviceWorkerRegistration.register();

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
