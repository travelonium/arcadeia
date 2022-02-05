import React, { Component } from 'react';
import Library from './components/Library';
import { Layout } from './components/Layout';
import { Route } from 'react-router';

export default class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        this.library = React.createRef();
        this.state = {
            darkMode: window.matchMedia('(prefers-color-scheme: dark)').matches ? true : false
        };
        // add a class to the <html> tag specifying whether we should use the dark or the light theme
        const themeClassName = this.state.darkMode ? 'dark' : 'light';
        if (!document.documentElement.classList.contains(themeClassName)) {
            document.documentElement.className += ` ${themeClassName}`;
        }
    }

    componentDidMount() {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => this.onSelectMode(e.matches ? 'dark' : 'light'));
    }

    componentWillUnmount() {
        window.matchMedia('(prefers-color-scheme: dark)').removeEventListener('change', e => this.onSelectMode(this, e.matches ? 'dark' : 'light'));
    }

    onSelectMode(mode) {
        if (mode === 'light') {
            document.documentElement.classList.remove('dark');
        } else {
            document.documentElement.classList.remove('light');
        }
        document.documentElement.className += ` ${mode}`;
        this.setState({
            darkMode: ((mode === 'dark') ? true : false)
        });
    }

    render() {
        return (
            <Layout library={this.library} darkMode={this.state.darkMode}>
                <Route exact path='/*' render={(props) => <Library {...props} ref={this.library} forwardedRef={this.library} darkMode={this.state.darkMode} /> } />
            </Layout>
        );
    }
}

