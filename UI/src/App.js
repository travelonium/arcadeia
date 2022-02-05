import React, { Component } from 'react';
import Library from './components/Library';
import { setTheme } from './features/ui/slice';
import { Layout } from './components/Layout';
import { connect } from "react-redux";
import { Route } from 'react-router';

class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        this.library = React.createRef();
        // initialize the theme setting it to dark/light mode
        this.onSelectMode();
    }

    componentDidMount() {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => this.onSelectMode(e.matches ? 'dark' : 'light'));
    }

    componentWillUnmount() {
        window.matchMedia('(prefers-color-scheme: dark)').removeEventListener('change', e => this.onSelectMode(this, e.matches ? 'dark' : 'light'));
    }

    onSelectMode(mode) {
        if (!mode) mode = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        this.props.dispatch(setTheme(mode));
    }

    render() {
        return (
            <Layout library={this.library}>
                <Route exact path='/*' render={(props) => <Library {...props} ref={this.library} forwardedRef={this.library} /> } />
            </Layout>
        );
    }
}

const mapStateToProps = (state) => ({
    ui: {
        theme: state.ui.theme,
    }
});

export default connect(mapStateToProps, null, null)(App);

