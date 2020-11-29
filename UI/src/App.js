import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Library } from './components/Library';
import { FetchData } from './components/FetchData';
import { Counter } from './components/Counter';

export default class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        this.library = React.createRef();
        this.navigation = React.createRef();
    }

    render() {
        return (
            <Layout library={this.library} navigation={this.navigation}>
                <Route exact path='/*' render={(props) => <Library {...props} ref={this.library} navigation={this.navigation} /> } />
                <Route path='/counter' component={Counter} />
                <Route path='/fetch-data' component={FetchData} />
            </Layout>
        );
    }
}
