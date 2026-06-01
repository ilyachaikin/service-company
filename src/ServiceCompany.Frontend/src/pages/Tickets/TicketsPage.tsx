import React, { useState, useEffect } from 'react';
import { Table, Button, Input, Space, Tag, message, Card, Typography, Select, Tooltip, Tabs } from 'antd';
import { PlusOutlined, SearchOutlined, EyeOutlined, DownloadOutlined, UserOutlined, InboxOutlined } from '@ant-design/icons';
import api from '../../api/client';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../store/AuthContext';
import dayjs from 'dayjs';

const { Title } = Typography;

const statusColors: Record<number, { color: string; label: string }> = {
  0: { color: 'cyan',       label: 'Новая' },
  1: { color: 'blue',       label: 'Назначена' },
  2: { color: 'processing', label: 'В работе' },
  3: { color: 'warning',    label: 'Ожидание запчастей' },
  4: { color: 'success',    label: 'Выполнена' },
  5: { color: 'default',    label: 'Закрыта' },
  6: { color: 'default',    label: 'Отменена' },
  7: { color: 'purple',     label: 'Архив' },
};

const priorityColors: Record<number, { color: string; label: string }> = {
  0: { color: 'red',     label: 'Критический' },
  1: { color: 'volcano', label: 'Высокий' },
  2: { color: 'gold',    label: 'Обычный' },
  3: { color: 'green',   label: 'Низкий' },
};

const TicketsPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const isEngineer = user?.roles?.includes('Engineer') && !user.roles.includes('Admin') && !user.roles.includes('Manager');

  const [activeTab, setActiveTab] = useState<'active' | 'archive'>('active');
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [params, setParams] = useState<any>({
    page: 1,
    pageSize: 15,
    searchTerm: '',
    status: undefined,
    priority: undefined,
    assignedUserId: isEngineer ? user?.id : undefined,
  });

  const fetchTickets = async (tab: 'active' | 'archive' = activeTab, currentParams = params) => {
    setLoading(true);
    try {
      const queryParams = { ...currentParams };
      if (tab === 'archive') {
        queryParams.status = 7;
      } else if (queryParams.status === 7) {
        queryParams.status = undefined;
      }
      const res = await api.get('/Tickets', { params: queryParams });
      setData(res.data.items);
      setTotal(res.data.totalCount);
    } catch {
      message.error('Не удалось загрузить заявки');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchTickets(activeTab, params); }, [params, activeTab]);

  const handleTabChange = (key: string) => {
    setActiveTab(key as 'active' | 'archive');
    setParams((p: any) => ({ ...p, status: undefined, page: 1 }));
  };

  const handleExport = async () => {
    try {
      const exportParams = { ...params, page: 1, pageSize: 10000 };
      if (activeTab === 'archive') exportParams.status = 7;
      const res = await api.get('/Tickets/export/xlsx', {
        params: exportParams,
        responseType: 'blob',
      });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `Заявки_${dayjs().format('YYYYMMDD_HHmm')}.xlsx`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch {
      message.error('Не удалось экспортировать заявки');
    }
  };

  const columns = [
    {
      title: 'Тема',
      dataIndex: 'title',
      key: 'title',
      render: (text: string, record: any) => (
        <div
          onClick={() => navigate(`/tickets/${record.id}`)}
          style={{ cursor: 'pointer', fontWeight: 500, color: '#1890ff' }}
        >
          {text}
        </div>
      ),
    },
    { title: 'Клиент', dataIndex: 'clientName', key: 'clientName' },
    {
      title: 'Статус',
      dataIndex: 'status',
      key: 'status',
      render: (s: number) => <Tag color={statusColors[s]?.color}>{statusColors[s]?.label}</Tag>,
    },
    {
      title: 'Приоритет',
      dataIndex: 'priority',
      key: 'priority',
      render: (p: number) => <Tag color={priorityColors[p]?.color}>{priorityColors[p]?.label}</Tag>,
    },
    {
      title: 'Исполнитель',
      dataIndex: 'assignedUserName',
      key: 'assignedUserName',
      render: (name: string | null) =>
        name ? (
          <Space size={4}>
            <UserOutlined style={{ color: '#1890ff' }} />
            <span>{name}</span>
          </Space>
        ) : (
          <Tag color="default">Не назначен</Tag>
        ),
    },
    {
      title: 'Создана',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => dayjs(date).format('DD.MM.YYYY HH:mm'),
    },
    {
      title: 'Действия',
      key: 'action',
      render: (_: any, record: any) => (
        <Button size="small" icon={<EyeOutlined />} onClick={() => navigate(`/tickets/${record.id}`)}>
          Открыть
        </Button>
      ),
    },
  ];

  const filters = (
    <div className="filter-bar" style={{ marginBottom: 16, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
      <Input
        placeholder="Поиск заявок..."
        prefix={<SearchOutlined />}
        style={{ width: 250, minWidth: 160 }}
        onChange={(e) => setParams((p: any) => ({ ...p, searchTerm: e.target.value, page: 1 }))}
      />
      {activeTab === 'active' && (
        <Select
          placeholder="Фильтр по статусу"
          style={{ width: 200, minWidth: 140 }}
          allowClear
          onChange={(val) => setParams((p: any) => ({ ...p, status: val, page: 1 }))}
        >
          {Object.entries(statusColors)
            .filter(([key]) => parseInt(key) !== 7)
            .map(([key, val]) => (
              <Select.Option key={key} value={parseInt(key)}>{val.label}</Select.Option>
            ))}
        </Select>
      )}
      <Select
        placeholder="Фильтр по приоритету"
        style={{ width: 180, minWidth: 140 }}
        allowClear
        onChange={(val) => setParams((p: any) => ({ ...p, priority: val, page: 1 }))}
      >
        {Object.entries(priorityColors).map(([key, val]) => (
          <Select.Option key={key} value={parseInt(key)}>{val.label}</Select.Option>
        ))}
      </Select>
    </div>
  );

  const table = (
    <>
      {filters}
      {isEngineer && (
        <div style={{ marginBottom: 12, color: '#888', fontSize: 13 }}>
          Показаны только заявки, назначенные вам.
        </div>
      )}
      <Table
        columns={columns}
        dataSource={data}
        loading={loading}
        rowKey="id"
        scroll={{ x: 750 }}
        pagination={{
          current: params.page,
          pageSize: params.pageSize,
          total,
          onChange: (page, pageSize) => setParams((p: any) => ({ ...p, page, pageSize })),
        }}
      />
    </>
  );

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Заявки</Title>
        <Space>
          <Tooltip title="Экспорт в Excel">
            <Button icon={<DownloadOutlined />} onClick={handleExport}>
              Экспорт XLSX
            </Button>
          </Tooltip>
          {!isEngineer && (
            <Button type="primary" size="large" icon={<PlusOutlined />} onClick={() => navigate('/tickets/create')}>
              Создать заявку
            </Button>
          )}
        </Space>
      </div>

      <Card bodyStyle={{ paddingTop: 0 }}>
        <Tabs
          activeKey={activeTab}
          onChange={handleTabChange}
          items={[
            {
              key: 'active',
              label: <Space><span>Активные</span></Space>,
              children: table,
            },
            {
              key: 'archive',
              label: (
                <Space>
                  <InboxOutlined />
                  <span>Архив</span>
                </Space>
              ),
              children: table,
            },
          ]}
        />
      </Card>
    </div>
  );
};

export default TicketsPage;
