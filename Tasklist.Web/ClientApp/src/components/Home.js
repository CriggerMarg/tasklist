import React, { Component } from 'react';
import DataTable from 'react-data-table-component';
export class Home extends Component {
  constructor(props) {
    super(props);

    this.state = {
      loading: true,
    };
  }
  componentDidMount() {
    fetch('https://localhost:44336/tasklist')
      .then((res) => res.json())
      .then(
        (result) => {
          this.setState({
            isLoaded: true,
            items: result,
          });
        },
        // Примечание: важно обрабатывать ошибки именно здесь, а не в блоке catch(),
        // чтобы не перехватывать исключения из ошибок в самих компонентах.
        (error) => {
          this.setState({
            isLoaded: true,
            error,
          });
        }
      );
  }
  render() {
    const { error, isLoaded, items } = this.state;

    const columns = [
      {
        name: 'Name',
        selector: 'name',
        width: 200,
        sortable: true,
      },
      {
        name: 'CPU',
        selector: 'cpuLoad',
        width: 100,
        sortable: true,
      },
    ];
    if (error) {
      return <div>Error: {error.message}</div>;
    } else if (!isLoaded) {
      return <div>Loading...</div>;
    } else {
      return (
        <div>
          <DataTable
            title="Task manager"
            columns={columns}
            data={items}
            defaultSortAsc={false}
            defaultSortField="cpuload"
          />
        </div>
      );
    }
  }
}
