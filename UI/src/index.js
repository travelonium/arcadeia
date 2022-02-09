import App from './App';
import React from 'react';
import ReactDOM from 'react-dom';
import { store, persistor } from './store';
import { Provider } from 'react-redux';
import { ToastContainer } from 'react-toastify';
import { BrowserRouter } from 'react-router-dom';
import { PersistGate } from 'redux-persist/integration/react';
import registerServiceWorker from './registerServiceWorker';
import './stylesheet.scss';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

ReactDOM.render(
    <BrowserRouter basename={baseUrl}>
        <Provider store={store}>
            <PersistGate loading={null} persistor={persistor}>
                <App />
            </PersistGate>
        </Provider>
        <ToastContainer position="bottom-right" theme="colored" />
    </BrowserRouter>,
    rootElement
);

registerServiceWorker();

